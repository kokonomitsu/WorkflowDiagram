﻿using Crypto.Core;
using Crypto.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowDiagram.Nodes.Base;
using WorkflowDiagram;

namespace WorkflowDiagramApp.StrategyDocument {
    public class WfTickerCandlestickHistory : WfVisualNodeBase {
        public override string VisualTemplateName => "CandlestickHistory";

        public override string Type => "Candles";

        public override string Category => "Data";

        public override void OnVisit(WfRunner runner) {
            Ticker ticker = Inputs[0].Value as Ticker;
            if(ticker == null) {
                var res = new ResizeableArray<CandleStickData>();
                Outputs[0].OnVisit(runner, res);
                DataContext = res;
                return;
            }
            long seconds = (long)((End - Start).TotalSeconds);
            var candles = ticker.Exchange.GetCandleStickData(ticker, ticker.CandleStickPeriodMin, Start, seconds);
            Outputs[0].OnVisit(runner, candles);
            DataContext = candles;
        }

        protected override List<WfConnectionPoint> GetDefaultInputs() {
            return new WfConnectionPoint[] {
                new WfConnectionPoint() { Type = WfConnectionPointType.In, Name = "Ticker", Text = "Ticker", Requirement = WfRequirementType.Optional  }
            }.ToList();
        }

        protected override List<WfConnectionPoint> GetDefaultOutputs() {
            return new WfConnectionPoint[] {
                new WfConnectionPoint() { Type = WfConnectionPointType.In, Name = "Candles", Text = "Candles", Requirement = WfRequirementType.Optional  }
            }.ToList();
        }

        protected override bool OnInitializeCore(WfRunner runner) {
            return true;
        }

        public DateTime Start {
            get; set;
        }

        public DateTime End {
            get; set;
        }
    }
}
