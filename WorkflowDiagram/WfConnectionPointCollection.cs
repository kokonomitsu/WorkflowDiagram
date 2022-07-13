﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowDiagram {
    public class WfConnectionPointCollection : ObservableCollection<WfConnectionPoint> {
        public WfConnectionPointCollection(WfNode holder, WfConnectionPointType type) {
            Node = holder;
            Type = type;
        }

        public WfConnectionPoint this[string name] { get { return this.FirstOrDefault(p => p.Name == name); } }
        public WfNode Node { get; private set; }
        public WfConnectionPointType Type { get; private set; }
        protected override void InsertItem(int index, WfConnectionPoint item) {
            item.Type = Type;
            WfConnectionPoint prev = this.FirstOrDefault(p => p.Name == item.Name);
            if(prev != null) {
                this[IndexOf(prev)] = item;
                return;
            }
            base.InsertItem(index, item);
            item.Collection = this;
            Node.OnPointsChanged(this);
        }
        protected override void RemoveItem(int index) {
            this[index].Collection = null;
            base.RemoveItem(index);
            Node.OnPointsChanged(this);
        }
        protected override void SetItem(int index, WfConnectionPoint item) {
            this[index].Collection = null;
            item.Collection = this;
            base.SetItem(index, item);
            Node.OnPointsChanged(this);
        }
        public void Add(IEnumerable<WfConnectionPoint> points) {
            foreach(var point in points) {
                Add(point);
            }
        }
    }
}