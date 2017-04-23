using System;
using System.Fabric;
using System.ServiceModel;
using Common;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;

namespace SimpleStoreClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri serviceName = new Uri("fabric:/SimpleStoreApplication/ShoppingCartService");
            ServicePartitionResolver serviceResolver = new ServicePartitionResolver(() => new FabricClient());
            NetTcpBinding binding = CreateClientConnectionBinding();

            for (int i = 0; i < 10; i++)
            {
                Client shoppingClient = new Client(
                    new WcfCommunicationClientFactory<IShoppingCartService>(binding, null, serviceResolver),
                    serviceName, i);

                shoppingClient.AddItem(new ShoppingCartItem
                {
                    ProductName = "XBOX ONE (" + i + ")",
                    UnitPrice = 329.0,
                    Amount = 2
                }).Wait();

                shoppingClient.AddItem(new ShoppingCartItem
                {
                    ProductName = "Samsung TV (" + i + ")",
                    UnitPrice = 1032.99,
                    Amount = 1
                }).Wait();

                PrintPartition(shoppingClient);

                var list = shoppingClient.GetItems().Result;
                foreach (var item in list)
                {
                    Console.WriteLine($"{item.ProductName}: {item.UnitPrice:C2} X {item.Amount} = {item.LineTotal:C2}");
                }
            }
            Console.ReadKey();
        }

        private static void PrintPartition(Client client)
        {
            ResolvedServicePartition partition;
            if (client.TryGetLastResolvedServicePartition(out partition))
            {
                Console.WriteLine("Partition ID: " + partition.Info.Id);
            }
        }

        private static NetTcpBinding CreateClientConnectionBinding()
        {
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None)
            {
                SendTimeout = TimeSpan.MaxValue,
                ReceiveTimeout = TimeSpan.MaxValue,
                OpenTimeout = TimeSpan.FromSeconds(5),
                CloseTimeout = TimeSpan.FromSeconds(5),
                MaxReceivedMessageSize = 1024 * 1024
            };
            binding.MaxBufferSize = (int)binding.MaxReceivedMessageSize;
            binding.MaxBufferPoolSize = Environment.ProcessorCount * binding.MaxReceivedMessageSize;

            return binding;
        }
    }
}
