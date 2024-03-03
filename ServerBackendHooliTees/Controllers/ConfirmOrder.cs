﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerBackendHooliTees.Models.Database;
using ServerBackendHooliTees.Models.Database.Entities;
using ServerBackendHooliTees.Services;
using System.Numerics;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.TransactionReceipts;

namespace ServerBackendHooliTees.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfirmOrder : ControllerBase
    {

        //carteraPortatilJRR = "0x2FB33CAA5ad35ee90dA38254502b5576BF3eBF10";
        //carteraCasaJRR = "0x123D68808120295662d794753E3519202B1F1Fc2";

        private const string OUR_WALLET = "0x2FB33CAA5ad35ee90dA38254502b5576BF3eBF10";
        private const string NETWORK_URL = "https://rpc.sepolia.org";

        private readonly MyDbContext _hooliteesDataBase;

        public ConfirmOrder(MyDbContext hooliteesDataBase)
        {
            _hooliteesDataBase = hooliteesDataBase;
        }

        [HttpPost("buyProducts")]
        public async Task<TransactionToSing> BuyAsync([FromForm] string clientWallet, [FromForm] decimal totalPrice ,[FromForm] int userID)
        {
            CartProducts cartProducts = _hooliteesDataBase.CartProducts
                .FirstOrDefault(id => id.ShoppingCartId  == userID);

            using CoincGeckoApi coincGeckoApi = new CoincGeckoApi();
            decimal ethereumEur = await coincGeckoApi.GetEthereumPriceAsync();
            BigInteger priceWei = Web3.Convert.ToWei(totalPrice / ethereumEur);

            Web3 web3 = new Web3(NETWORK_URL);

            TransactionToSing transactionToSing = new TransactionToSing()
            {
                From = clientWallet,
                To = OUR_WALLET,
                Value = new HexBigInteger(priceWei).HexValue,
                Gas = new HexBigInteger(30000).HexValue,
                GasPrice = (await web3.Eth.GasPrice.SendRequestAsync()).HexValue
            };

            Transaction transaction = new Transaction()
            {
                ClientWallet = transactionToSing.From,
                Value = transactionToSing.Value,
                userId = userID,

            };



            await _hooliteesDataBase.Transactions.AddAsync(transaction);
            await _hooliteesDataBase.SaveChangesAsync();
            transactionToSing.Id = transaction.Id;


            return transactionToSing;
        }


        [HttpPost("check/{transactionID}")]
        public async Task<bool> CheckTransactionAsync(int transactionId, [FromBody] string txHash)
        {
            bool success = false;
            Transaction transaction = await _hooliteesDataBase.Transactions.FirstOrDefaultAsync(id => id.Id == transactionId);
            Console.WriteLine(transaction);
            transaction.Hash = txHash;
            Console.WriteLine(txHash);
            Console.WriteLine(transaction.Hash);

            Web3 web3 = new Web3(NETWORK_URL);
            var receiptPollingService = new TransactionReceiptPollingService(
                web3.TransactionManager, 1000);

            try
            {
                // Esperar a que la transacción se confirme en la cadena de bloques
                var transactionReceipt = await receiptPollingService.PollForReceiptAsync(txHash);

                // Obtener los datos de la transacción
                var transactionEth = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);

                Console.WriteLine(transactionEth.TransactionHash == transactionReceipt.TransactionHash);
                Console.WriteLine(transactionReceipt.Status.Value == 1);
                Console.WriteLine(transactionReceipt.TransactionHash == transaction.Hash);
                Console.WriteLine(transactionReceipt.From == transaction.ClientWallet);
                Console.WriteLine(transactionReceipt.To.Equals(OUR_WALLET, StringComparison.OrdinalIgnoreCase));

                success = transactionEth.TransactionHash == transactionReceipt.TransactionHash
                    && transactionReceipt.Status.Value == 1 // Transacción realizada con éxito
                    && transactionReceipt.TransactionHash == transaction.Hash // El hash es el mismo
                    && transactionReceipt.From == transaction.ClientWallet // El dinero viene del cliente
                    && transactionReceipt.To.Equals(OUR_WALLET, StringComparison.OrdinalIgnoreCase); // El dinero se ingresa en nuestra cuenta
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al esperar la transacción: {ex.Message}");
            }

            

            if (success)
            {
                CartProducts[] productsCarrito2 = _hooliteesDataBase.CartProducts.Where(p => p.ShoppingCartId == transaction.userId).ToArray();

                CartProducts[] productsCarrito = [.. _hooliteesDataBase.CartProducts.Where(p => p.ShoppingCartId == transaction.userId)];

                Console.WriteLine(productsCarrito.Length);
                Console.WriteLine(productsCarrito2.Length);

                for (int i = 0; i < productsCarrito.Length; i++)
                {
                    Console.WriteLine("i:"+i);
                    ProductOrder nuevoProductOrder = new ProductOrder()
                    {
                        ProductsId = productsCarrito[i].ProductId,
                        Quantity = productsCarrito[i].Quantity,
                        OrdersId = transaction.Id.ToString(),
                    };
                    Console.WriteLine("orderID:" + nuevoProductOrder.OrdersId);
                    await _hooliteesDataBase.ProductOrders.AddAsync(nuevoProductOrder);
                }


                for (int i = 0; i < productsCarrito.Length; i++)
                {
                    _hooliteesDataBase.CartProducts.Remove(_hooliteesDataBase.CartProducts.FirstOrDefault(p => p.ShoppingCartId == transaction.userId));
                    await _hooliteesDataBase.SaveChangesAsync();
                }
            }
            transaction.Completed = success;
            await _hooliteesDataBase.SaveChangesAsync();

            return success;
        }

        [HttpPost("precioETH")]
        public async Task<decimal> GetPrecio([FromForm] decimal totalPrice)
        {
            using CoincGeckoApi coincGeckoApi = new CoincGeckoApi();
            decimal ethereumEur = await coincGeckoApi.GetEthereumPriceAsync();
            decimal result = (totalPrice / ethereumEur);
            return result;
        }


    }
}
