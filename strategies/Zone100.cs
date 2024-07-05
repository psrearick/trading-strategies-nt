#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class Zone100 : Strategy
	{
		private class Zone
	    {
	        public double Price { get; set; }
	        public int StartBar { get; set; }
	        public int EndBar { get; set; }
	        public bool IsSupply { get; set; }
	        public double ZoneSize { get; set; }
	        public double Volume { get; set; }
	        public int TouchCount { get; set; }
	        public string DrawObjectName { get; set; }
	    }

	    private List<Zone> zones = new List<Zone>();
		private List<Zone> inactiveZones = new List<Zone>();
		int SupplyZoneOpacity = 60;
        int DemandZoneOpacity = 60;

	    private double averageVolume = 0;
		private EMA trendEma;

		private DateTime OpenTime = DateTime.Parse(
            "10:00",
            System.Globalization.CultureInfo.InvariantCulture
        );
        private DateTime CloseTime = DateTime.Parse(
            "15:30",
            System.Globalization.CultureInfo.InvariantCulture
        );
        private DateTime LastTradeTime = DateTime.Parse(
            "15:00",
            System.Globalization.CultureInfo.InvariantCulture
        );

	 	[NinjaScriptProperty]
	    [Range(1, int.MaxValue)]
	    [Display(Name="Lookback Period", Description="Number of bars to look back for zone identification", Order=1, GroupName="Parameters")]
	    public int LookbackPeriod { get; set; }

	    [NinjaScriptProperty]
	    [Range(1, int.MaxValue)]
	    [Display(Name="Zone Threshold", Description="Number of bars to determine swing high/low", Order=2, GroupName="Parameters")]
	    public int ZoneThreshold { get; set; }

	    [NinjaScriptProperty]
	    [Range(0.25, double.MaxValue)]
	    [Display(Name="Zone Size (Points)", Description="Size of supply/demand zone in points", Order=3, GroupName="Parameters")]
	    public double ZoneSizePoints { get; set; }

	    [NinjaScriptProperty]
	    [Range(1, double.MaxValue)]
	    [Display(Name="Stop Loss Multiplier", Description="Multiplier for stop loss distance", Order=4, GroupName="Parameters")]
	    public double StopLossMultiplier { get; set; }

	    [NinjaScriptProperty]
	    [Range(1, int.MaxValue)]
	    [Display(Name="Volume Threshold Multiplier", Description="Multiplier for average volume to identify significant zones", Order=5, GroupName="Parameters")]
	    public double VolumeThresholdMultiplier { get; set; }

	    [NinjaScriptProperty]
	    [Range(1, int.MaxValue)]
	    [Display(Name="Max Zone Touch Count", Description="Maximum number of times a zone can be touched before being invalidated", Order=6, GroupName="Parameters")]
	    public int MaxZoneTouchCount { get; set; }

		[NinjaScriptProperty]
		[Range(10, 100)]
		[Display(Name="Max Stop Loss (Ticks)", Description="Maximum stop loss size in ticks", Order=7, GroupName="Parameters")]
		public int MaxStopLossTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Draw Zones on Chart", Description="Draw Zones on Chart", Order=8, GroupName="Parameters")]
		public bool ShouldDrawZones { get; set; }

		[NinjaScriptProperty]
		[Range(10, 200)]
		[Display(Name="Trend EMA Period", Description="EMA period for trend identification", Order=9, GroupName="Parameters")]
		public int TrendEmaPeriod { get; set; }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Zone 1.0.0";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= true;

	            LookbackPeriod = 20; // 20
	            ZoneThreshold = 10; // 5
	            ZoneSizePoints = 2.5; // 1.5
	            StopLossMultiplier = 20; // 2
	            VolumeThresholdMultiplier = 1.5; // 1.5
	            MaxZoneTouchCount = 3; // 3
				MaxStopLossTicks = 50; // 50
				ShouldDrawZones = false;
				TrendEmaPeriod = 50;
			}
			else if (State == State.Configure)
			{
			}
		    else if (State == State.DataLoaded)
		    {
		        trendEma = EMA(Close, TrendEmaPeriod);
		    }
			else if (State == State.Terminated)
	        {
	            RemoveDrawObjects();
	        }
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Math.Max(BarsRequiredToTrade, LookbackPeriod))
            return;

			ExitPositions();

			averageVolume = SMA(Volume, LookbackPeriod)[0];

	        if (IsSignificantSwingHigh())
	        {
	            var newZone = new Zone
	            {
	                Price = High[0],
	                StartBar = CurrentBar,
	                EndBar = CurrentBar,
	                IsSupply = true,
	                ZoneSize = ZoneSizePoints * TickSize,
	                Volume = Volume[0],
	                TouchCount = 0,
	                DrawObjectName = "SupplyZone_" + CurrentBar
	            };
	            zones.Add(newZone);
	            DrawZone(newZone);
	        }
	        else if (IsSignificantSwingLow())
	        {
	            var newZone = new Zone
	            {
	                Price = Low[0],
	                StartBar = CurrentBar,
	                EndBar = CurrentBar,
	                IsSupply = false,
	                ZoneSize = ZoneSizePoints * TickSize,
	                Volume = Volume[0],
	                TouchCount = 0,
	                DrawObjectName = "DemandZone_" + CurrentBar
	            };
	            zones.Add(newZone);
	            DrawZone(newZone);
	        }

	        UpdateAndCleanupZones();

			if (!IsValidEntryTime()) {
				return;
            }

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

	        foreach (var zone in zones)
	        {
	            if (zone.IsSupply && Close[0] > zone.Price && Close[1] <= zone.Price)
	            {
	                EnterShortWithDynamicExits(zone);
	            }
	            else if (!zone.IsSupply && Close[0] < zone.Price && Close[1] >= zone.Price)
	            {
	                EnterLongWithDynamicExits(zone);
	            }
	        }
		}

		private bool IsSignificantSwingHigh()
		{
		    return High[0] == MAX(High, ZoneThreshold)[0]
		        && Volume[0] > averageVolume * VolumeThresholdMultiplier
				&& Close[0] > trendEma[0];
		}

		private bool IsSignificantSwingLow()
		{
		    return Low[0] == MIN(Low, ZoneThreshold)[0]
		        && Volume[0] > averageVolume * VolumeThresholdMultiplier
				&& Close[0] < trendEma[0];
		}

		private void DrawZone(Zone zone)
	    {
			if (!ShouldDrawZones)
			{
				return;
			}

	        double upperBound = zone.IsSupply ? zone.Price + zone.ZoneSize : zone.Price;
	        double lowerBound = zone.IsSupply ? zone.Price : zone.Price - zone.ZoneSize;

	        Draw.Rectangle(this, zone.DrawObjectName, true, zone.StartBar, upperBound, zone.EndBar, lowerBound,
	            zone.IsSupply ? Brushes.Red : Brushes.Green,
	            zone.IsSupply ? Brushes.Red.Clone() : Brushes.Green.Clone(),
	            zone.IsSupply ? SupplyZoneOpacity : DemandZoneOpacity);
	    }

	   	private void UpdateAndCleanupZones()
	    {
	        List<Zone> zonesToRemove = new List<Zone>();

	        foreach (var zone in zones)
	        {
	            if (CurrentBar - zone.StartBar > LookbackPeriod)
	            {
	                zonesToRemove.Add(zone);
	                inactiveZones.Add(zone);
	            }
	            else if ((zone.IsSupply && Math.Abs(High[0] - zone.Price) <= zone.ZoneSize) ||
	                     (!zone.IsSupply && Math.Abs(Low[0] - zone.Price) <= zone.ZoneSize))
	            {
	                zone.EndBar = CurrentBar;
	                zone.TouchCount++;
	                DrawZone(zone);

	                if (zone.TouchCount >= MaxZoneTouchCount)
	                {
	                    zonesToRemove.Add(zone);
	                    inactiveZones.Add(zone);
	                }
	            }
	        }

	        foreach (var zone in zonesToRemove)
	        {
	            zones.Remove(zone);
	        }
	    }

		private void EnterLongWithDynamicExits(Zone entryZone)
		{
		    double stopLossPrice = Math.Min(
		        entryZone.Price - (entryZone.ZoneSize * StopLossMultiplier),
		        Low[0] - (10 * TickSize)  // Ensure at least 10 ticks below the current low
		    );

			double maxStopLossDistance = MaxStopLossTicks * TickSize;
			if (Math.Abs(stopLossPrice - Close[0]) > maxStopLossDistance)
			{
			    stopLossPrice = Close[0] + (entryZone.IsSupply ? -maxStopLossDistance : maxStopLossDistance);
			}

		    double takeProfitPrice = Math.Min(FindNextSupplyZone(entryZone.Price), Close[0] + 20);

		    // Ensure minimum stop loss of 10 ticks
		    if (Close[0] - stopLossPrice < 10 * TickSize)
		    {
		        stopLossPrice = Close[0] - (10 * TickSize);
		    }

		    SetStopLoss(CalculationMode.Price, stopLossPrice);
		    SetProfitTarget(CalculationMode.Price, takeProfitPrice);
		    EnterLong();
		}

	    private void EnterShortWithDynamicExits(Zone entryZone)
		{
		    double stopLossPrice = Math.Max(
		        entryZone.Price + (entryZone.ZoneSize * StopLossMultiplier),
		        High[0] + (10 * TickSize)  // Ensure at least 10 ticks above the current high
		    );

			double maxStopLossDistance = MaxStopLossTicks * TickSize;
			if (Math.Abs(stopLossPrice - Close[0]) > maxStopLossDistance)
			{
			    stopLossPrice = Close[0] + (entryZone.IsSupply ? -maxStopLossDistance : maxStopLossDistance);
			}

		    double takeProfitPrice = Math.Max(FindNextDemandZone(entryZone.Price), Close[0] - 20);

		    // Ensure minimum stop loss of 10 ticks
		    if (stopLossPrice - Close[0] < 10 * TickSize)
		    {
		        stopLossPrice = Close[0] + (10 * TickSize);
		    }

		    SetStopLoss(CalculationMode.Price, stopLossPrice);
		    SetProfitTarget(CalculationMode.Price, takeProfitPrice);
		    EnterShort();
		}

	    private double FindNextSupplyZone(double currentPrice)
	    {
	        var nextSupplyZone = zones
	            .Where(z => z.IsSupply && z.Price > currentPrice)
	            .OrderBy(z => z.Price)
	            .FirstOrDefault();

	        return nextSupplyZone != null ? nextSupplyZone.Price : currentPrice + (10 * TickSize);
	    }

	    private double FindNextDemandZone(double currentPrice)
	    {
	        var nextDemandZone = zones
	            .Where(z => !z.IsSupply && z.Price < currentPrice)
	            .OrderByDescending(z => z.Price)
	            .FirstOrDefault();

	        return nextDemandZone != null ? nextDemandZone.Price : currentPrice - (10 * TickSize);
	    }

		private void RemoveDrawObjects()
	    {
	        foreach (var zone in zones)
	        {
	            RemoveDrawObject(zone.DrawObjectName);
	        }
	    }

		#region ExitPositions()
		private void ExitPositions()
		{
			if (IsValidTradeTime()) {
				return;
			}

			if (Position.MarketPosition == MarketPosition.Long) {
				ExitLong();
			}

			if (Position.MarketPosition == MarketPosition.Short) {
				ExitShort();
			}
        }
		#endregion

		#region IsValidEntryTime()
		private bool IsValidEntryTime()
		{
			int now = ToTime(Time[0]);

			if (now < ToTime(OpenTime)) {
				return false;
			}

			if (now > ToTime(LastTradeTime)) {
				return false;
			}

			return true;
		}
		#endregion

		#region IsValidTradeTime()
		private bool IsValidTradeTime()
		{
			int now = ToTime(Time[0]);

			if (now > ToTime(CloseTime)) {
				return false;
			}

			if (now < ToTime(OpenTime)) {
				return false;
			}

			return true;
		}
		#endregion

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	    {
	        base.OnRender(chartControl, chartScale);

			if (!ShouldDrawZones)
			{
				return;
			}

	        foreach (var zone in inactiveZones)
	        {
	            double upperBound = zone.IsSupply ? zone.Price + zone.ZoneSize : zone.Price;
	            double lowerBound = zone.IsSupply ? zone.Price : zone.Price - zone.ZoneSize;

	            SharpDX.RectangleF rect = new SharpDX.RectangleF();
	            rect.X = chartControl.GetXByBarIndex(ChartBars, zone.StartBar);
	            rect.Y = (float)chartScale.GetYByValue(upperBound);
	            rect.Width = chartControl.GetXByBarIndex(ChartBars, zone.EndBar) - rect.X;
	            rect.Height = (float)chartScale.GetYByValue(lowerBound) - rect.Y;

	            SharpDX.Color color = zone.IsSupply ? SharpDX.Color.Red : SharpDX.Color.Green;
	            color.A = (byte)(zone.IsSupply ? SupplyZoneOpacity : DemandZoneOpacity);

	            RenderTarget.FillRectangle(rect, new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, color));
	        }
	    }
	}
}
