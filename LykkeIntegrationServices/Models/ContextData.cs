namespace LykkeIntegrationServices.Models
{
    namespace Core.BitCoin
    {
        public class GenerateWalletContextData
        {
            public string ClientId { get; set; }

            public static GenerateWalletContextData Create(string clientId)
            {
                return new GenerateWalletContextData
                {
                    ClientId = clientId
                };
            }
        }

        public class CashInOutContextData
        {
            public string ClientId { get; set; }
            public string CashOperationId { get; set; }
        }

        public class CashInContextData
        {
            public CashInContextData(string clientId, string pendingBalanceId)
            {
                ClientId = clientId;
                PendingBalanceId = pendingBalanceId;
            }

            public string ClientId { get; set; }
            public string PendingBalanceId { get; set; }
        }

        public class CashOutContextData
        {
            public string ClientId { get; set; }
            public string AssetId { get; set; }
            public string Address { get; set; }
            public double Amount { get; set; }
            public string CashOperationId { get; set; }


            public static CashOutContextData Create(string clientId, string assetId, string address, double amount,
                string cashOpId)
            {
                return new CashOutContextData
                {
                    ClientId = clientId,
                    AssetId = assetId,
                    Amount = amount,
                    Address = address,
                    CashOperationId = cashOpId
                };
            }

        }

        public class SwapContextData
        {
            public class OrderModel
            {
                public string ClientId { get; set; }
                public string OrderId { get; set; }
            }

            public class TradeModel
            {
                public string ClientId { get; set; }
                public string TradeId { get; set; }
            }

            public OrderModel MarketOrder { get; set; }
            public OrderModel ClientOrder { get; set; }
            public TradeModel[] Trades { get; set; }
        }

        public class TransferContextData
        {
            public class TransferModel
            {
                public string ClientId { get; set; }
                public string OperationId { get; set; }
                public string FiatAssetId { get; set; }
                public double Price { get; set; }
                public double AmountFiat { get; set; }
                public double AmountLkk { get; set; }

                public static TransferModel Create(string clientId, string operationId, string fiatAssetId,
                    double price, double amountFiat)
                {
                    return new TransferModel
                    {
                        ClientId = clientId,
                        OperationId = operationId,
                        FiatAssetId = fiatAssetId,
                        AmountFiat = amountFiat,
                        Price = price
                    };
                }

            }

            public TransferModel[] Transfers { get; set; }


            public static TransferContextData Create(params TransferModel[] transfers)
            {
                return new TransferContextData
                {
                    Transfers = transfers
                };
            }
        }

        public class RefundContextData
        {
            public string ClientId { get; set; }
            public string SrcBlockchainHash { get; set; }
            public string OperationType { get; set; }
            public double? Amount { get; set; }
            public string AssetId { get; set; }
        }
    }
}
