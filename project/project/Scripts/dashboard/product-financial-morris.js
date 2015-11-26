﻿$(function () {
    var product = document.getElementById("productID").getAttribute("value");

    $.ajax({
        dataType: "json",
        url: "http://localhost:49328/api/products/" + product + "/financial",
        success: function (financial) {
            financial = JSON.parse(financial);

            console.log(financial);
            var dataG = [];
            $.each(financial, function (i) {
                var mes = financial[i].Ano + "-" + financial[i].Mes;
                var profits = financial[i].Vendas - financial[i].Compras;
                dataG.push({ month: mes, sales: financial[i].Vendas, purchases: financial[i].Compras, profit: profits });
            });

            new Morris.Line({
                // ID of the element in which to draw the chart.
                element: 'productfinancial',
                // Chart data records -- each entry in this array corresponds to a point on
                // the chart.
                data: dataG,
                // The name of the data record attribute that contains x-values.
                xkey: 'month',
                // A list of names of data record attributes that contain y-values.
                ykeys: ['sales', 'purchases', 'profit'],
                // Labels for the ykeys -- will be displayed when you hover over the
                // chart.
                labels: ['Sales', 'Purchases', 'Profit'],
            });
        }
    })
});