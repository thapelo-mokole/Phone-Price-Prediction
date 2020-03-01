﻿using Microsoft.AspNetCore.Mvc;
using Prediction.Models.Enums;
using Prediction.Models.Hardware;
using Prediction.Models.NewChart;
using Prediction.Models.Time_Series_Forecasting;
using Prediction.Models.Time_Series_Forecasting.Cleaning;
using Prediction.View_Models.Chart.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



namespace Prediction.Models.ChartManual
{
    public class ManualChart
    {

        public ManualChart(List<Item> phones, List<PhoneProperties> hardware, List<int> selectedItems = null, int months = 12)
        {
            this.Hardware = hardware;
            this.SelectedItems = selectedItems.Distinct().ToList();
            this.Phones = phones;
            this.FutureForecastMonths = months;
        }

        public List<PhoneProperties> Hardware { get; set; } = new List<PhoneProperties>();
        public List<Item> Phones { get; set; } = new List<Item>();
        public List<int> SelectedItems { get; set; }
        public List<ChartItem> ChartItems { get; set; } = new List<ChartItem>();
        public List<string> Errors { get; set; } = new List<string>();
        public int FutureForecastMonths { get; set; }


        #region Generate Chart Data
        public List<ChartItem> DrawChart(List<int> hardwareId)
        {
            List<ChartItem> allCharts = new List<ChartItem>();

            // Itterates through all passed id
            for(int i = 0; i < hardwareId.Count; i++)
            {
                // Find brand and model corresponding to id
                var currentBrand = Hardware.FirstOrDefault(m => m.ConfigId == hardwareId[i]).Brand;
                var currentModel = Hardware.FirstOrDefault(m => m.ConfigId == hardwareId[i]).Model;

                // If there are enough transactions -> draws a chart and computers a forecasted price
                allCharts.Add(new ChartItem
                {
                    Label = $"{currentBrand.ToString()} {currentModel}",
                    Fill = false,
                    BorderWidth = 1,
                    // Computers price forecast. Returns a list of objects containing purchase date and price.
                    LstData = ComputeForecast(hardwareId[i])
                });
            }

            return allCharts;
        }

        private List<ChartTransaction> ComputeForecast(int id)
        {
            // A ChartTransaction object contains date of purchase and price.
            List<ChartTransaction> transactions = new List<ChartTransaction>();

            // Find brand and model based on passed id
            Brand selectedBrand = Hardware.FirstOrDefault(m => m.ConfigId == id).Brand;
            string selectedModel = Hardware.FirstOrDefault(m => m.ConfigId == id).Model;

            // Generates time series forecast.
            // RemoveUnforecastableIds method already checks if there are enough transactions to compute.
            int forecastMonths = 12;
            TimeSeriesPrediction prediction = new TimeSeriesPrediction(Phones, selectedBrand, selectedModel);
            prediction.GenerateFutureForecast(forecastMonths);


            List<ChartTransaction> transactionRecord = new List<ChartTransaction>();
            // Itterate through all objects
            foreach (Phone p in prediction.PhoneCollection.Phones)
            {
                // Adds to the List<Dict<int, ChartTrans>> so calculations can be done
                // In order to find the best/worst future price
                this.AddTransactionToRecord(id, new ChartTransaction { Date = p.Date, Price = p.Forecast.Value });

                // Records their date and price.
                transactions.Add(new ChartTransaction
                {
                    Date = p.Date,
                    Price = p.Forecast.Value
                });
            }

            return transactions;
        }
        #endregion

        #region Filter Unforecastable Ids
        public List<int> RemoveUnforecastableIds(List<int> hardwareId)
        {
            List<int> AcceptedIds = new List<int>();

            foreach(int id in hardwareId)
            {
                Brand selectedBrand = Hardware.FirstOrDefault(m => m.ConfigId == id).Brand;
                string selectedModel = Hardware.FirstOrDefault(m => m.ConfigId == id).Model;
                if (DataAuditing.HasEnoughTransactions(Phones, selectedBrand, selectedModel))
                {
                    AcceptedIds.Add(id);
                }
                else
                {
                    Errors.Add($"{selectedBrand} {selectedModel} - Not Enough Transactions");
                    Errors = Errors.Distinct().ToList();
                }
            }

            return AcceptedIds;
        }

        public List<int> RemoveDuplicateIds()
        {
            return SelectedItems.Distinct().ToList();
        }
        #endregion

        #region Select/Unselect
        public void UpdateSelectedItems(List<int> hardwareId)
        {
            foreach(PhoneProperties prop in Hardware)
            {
                if(hardwareId.Contains(prop.ConfigId))
                {
                    prop.isSelected = true;
                }
                else
                {
                    prop.isSelected = false;
                }
            }
        }
        #endregion

        private void AddTransactionToRecord(int id, ChartTransaction transaction)
        {

            if ( ForecastRecord._dict.ContainsKey(id))
            {
                ForecastRecord._dict[id].Add(transaction);
            }
            else
            {
                ForecastRecord._dict.Add(id, new List<ChartTransaction> { { transaction } });
            }

        }

    }
}