﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using project.Lib_Primavera.Model;
using project.Items;
using System.Globalization;

namespace project.Controllers
{
    public class ApiProductsController : ApiController
    {
        // GET: api/products/
        public IEnumerable<Lib_Primavera.Model.Artigo> Get()
        {
            return Lib_Primavera.PriIntegration.ListaArtigos();
        }

        // GET api/products/{id}
        public Artigo GetProduct(string id)
        {
            List<double> precos = new List<double>();
            double sumPrecos = 0;
            Lib_Primavera.Model.Artigo artigo = Lib_Primavera.PriIntegration.GetArtigo(id);
            if (artigo == null)
            {
                return null;
                //throw new HttpResponseException(
                // Request.CreateResponse(HttpStatusCode.NotFound));
            }
            else
            {
                artigo = Lib_Primavera.PriIntegration.GetPrecoArtigo(artigo);
                precos = Lib_Primavera.PriIntegration.GetTodosPrecosArtigo(artigo);
                foreach (double preco in precos)
                {
                    sumPrecos += preco;
                }
                artigo.PrecoMedio = sumPrecos / precos.Count;

                return artigo;
            }
        }

        // GET api/products/{id}/top-clients/{year}
        [System.Web.Http.HttpGet]
        public HttpResponseMessage TopClients(string id, int year)
        {

            List<CabecDoc> sales = Lib_Primavera.PriIntegration.GetTopClientesArtigo(id, year);
            List<TopClientsItem> result = new List<Items.TopClientsItem>();

            double totalSalesVolume = 0;

            foreach (CabecDoc sale in sales)
            {
                if (result.Exists(e => e.nif == sale.NumContribuinte))
                {
                    result.Find(e => e.nif == sale.NumContribuinte).salesVolume += (sale.TotalMerc + sale.TotalIva);
                    result.Find(e => e.nif == sale.NumContribuinte).numPurchases++;
                }
                else
                {
                    result.Add(new Items.TopClientsItem
                    {
                        entity = sale.Entidade,
                        name = sale.Nome,
                        nif = sale.NumContribuinte,
                        salesVolume = sale.TotalMerc + sale.TotalIva,
                        percentage = "",
                        numPurchases = 1
                    });
                }

                totalSalesVolume += (sale.TotalMerc + sale.TotalIva);
            }

            result = result.OrderBy(e => e.salesVolume).Reverse().Take(10).ToList();

            foreach (Items.TopClientsItem client in result)
                client.percentage += Math.Round(client.salesVolume / totalSalesVolume * 100, 2).ToString(CultureInfo.GetCultureInfo("en-GB"));
            var json = new JavaScriptSerializer().Serialize(result);

            return Request.CreateResponse(HttpStatusCode.OK, json);

        }

        // GET api/products/{id}/sales/{year}
        [System.Web.Http.HttpGet]
        public HttpResponseMessage Sales(string id, int year)
        {
            List<LinhaDocVenda> sales = Lib_Primavera.PriIntegration.GetVendasArtigo(id, year);
            double sum = 0;
            double totalQuantity = 0;
            ProductSalesItem result = new ProductSalesItem();
            foreach (LinhaDocVenda sale in sales)
            {
                sum += (sale.Quantidade * sale.PrecoUnitario);
                totalQuantity += sale.Quantidade;
            }

            result.Vendas = sum;
            result.Vendidos = totalQuantity;

            var json = new JavaScriptSerializer().Serialize(result);

            return Request.CreateResponse(HttpStatusCode.OK, json);

        }

        // GET api/products/{id}/purchases/{year}
        [System.Web.Http.HttpGet]
        public HttpResponseMessage Purchases(string id, int year)
        {
            List<LinhaDocCompra> purchases = Lib_Primavera.PriIntegration.GetComprasArtigo(id, year);
            double sum = 0;
            double totalQuantity = 0;

            ProductPurchasesItem result = new ProductPurchasesItem();
            foreach (LinhaDocCompra purchase in purchases)
            {
                sum += (Math.Abs(purchase.Quantidade) * purchase.PrecoUnitario);
                totalQuantity += Math.Abs(purchase.Quantidade);
            }

            result.Compras = sum;
            result.Comprados = totalQuantity;


            var json = new JavaScriptSerializer().Serialize(result);

            return Request.CreateResponse(HttpStatusCode.OK, json);

        }

        // GET api/products/{id}/financial/{year}
        [System.Web.Http.HttpGet]
        public HttpResponseMessage Financial(string id, int year)
        {
            string month = "";
            int m = 0;
            int y = 0;
            List<GlobalFinancialItem> global = new List<GlobalFinancialItem>();
            if (year == DateTime.Now.Year)
            {
                month = DateTime.Now.Month.ToString();
                m = Int32.Parse(month);
                y = year;
            }

            else
            {
                m = 12;
                y = year;
            }
            for (int i = 1; i < m + 1; i++)
            {
                double purchase = Lib_Primavera.PriIntegration.getMonthlyPurchases(i, y);
                double sale = Lib_Primavera.PriIntegration.getMonthlySales(i, y);
                global.Add(new GlobalFinancialItem
                {
                    Ano = y,
                    Mes = i,
                    Compras = Math.Abs(purchase),
                    Vendas = sale

                });
            }

            global = global.OrderBy(e => e.Mes).ToList();
            var json = new JavaScriptSerializer().Serialize(global);

            return Request.CreateResponse(HttpStatusCode.OK, json);


        }

        // GET api/products/top
        [System.Web.Http.HttpGet]
        public HttpResponseMessage TopProducts()
        {
            List<TopProductsItem> result;

            if (HomeController.top10ProductsCache != null)
            {
                result = HomeController.top10ProductsCache;
            }
            else
            {
                result = new List<TopProductsItem>();

                List<Lib_Primavera.Model.LinhaDocVenda> products = Lib_Primavera.PriIntegration.getProductSales();

                double totalSalesVolume = 0;

                foreach (LinhaDocVenda product in products)
                {
                    string cod = product.Artigo;
                    if (result.Exists(e => e.codArtigo == cod))
                    {
                        result.Find(e => e.codArtigo == cod).salesVolume += (product.PrecoLiquido);
                        result.Find(e => e.codArtigo == cod).quantity += product.Quantidade;
                    }
                    else
                    {
                        result.Add(new TopProductsItem
                        {
                            description = product.Descricao,
                            codArtigo = cod,
                            salesVolume = product.TotalILiquido,
                            quantity = product.Quantidade,
                            percentage = ""
                        });
                    }
                    totalSalesVolume += product.TotalILiquido;
                }


                result = result.OrderBy(e => e.salesVolume).Reverse().Take(10).ToList();

                foreach (TopProductsItem product in result)
                    product.percentage += Math.Round(product.salesVolume / totalSalesVolume * 100, 2).ToString(CultureInfo.GetCultureInfo("en-GB"));

                HomeController.top10ProductsCache = result;
            }

            var json = new JavaScriptSerializer().Serialize(result);

            return Request.CreateResponse(HttpStatusCode.OK, json);
        }

        // GET api/products/shipments
        [System.Web.Http.HttpGet]
        public HttpResponseMessage Shipments()
        {
            List<String> delayed = Lib_Primavera.PriIntegration.GetShipments();

            var json = new JavaScriptSerializer().Serialize(delayed.Count);

            return Request.CreateResponse(HttpStatusCode.OK, json);

        }

        // GET api/products/shipments/{id}
        [System.Web.Http.HttpGet]
        public HttpResponseMessage ProductShipments(string id)
        {
            List<String> delayed = Lib_Primavera.PriIntegration.GetProductShipments(id);

            var json = new JavaScriptSerializer().Serialize(delayed.Count);

            return Request.CreateResponse(HttpStatusCode.OK, json);

        }


    }
}
