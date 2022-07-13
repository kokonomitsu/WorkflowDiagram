﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WorkflowDiagram {
    
    [Serializable]
    [XmlInclude(typeof(WfConnectionPoint))]
    [XmlInclude(typeof(WfConnector))]
    public class WfDocument : INotifyPropertyChanged, ISupportSerialization {
        public WfDocument() {
            Id = Guid.NewGuid();

            Nodes = new WfNodeCollection(this);
            Connectors = new List<WfConnector>();

            InitializeDefaultColors();
        }

        [Browsable(false)]
        public Guid Id { get; set; }
        [Browsable(false)]
        public string FileName { get; set; }

        /// <summary>
        /// Settings, not related to scripts
        /// </summary>
        [Browsable(false)]
        public int FontSizeDelta { get; set; } = 0;

        public List<WfNode> GetStartNodes() {
            return Nodes.Where(n => !n.HasInputConnections).ToList();
        }

        public List<WfNode> GetEndNodes() {
            return Nodes.Where(n => !n.HasOutputConnections).ToList();
        }

        string name;
        public string Name {
            get { return name; }
            set {
                if(Name == value)
                    return;
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        string ISupportSerialization.FileName { get => FileName; set => FileName = value; }
        public event EventHandler Saved;
        public event EventHandler Loaded;

        public void Save() {
            if(string.IsNullOrEmpty(FullPath))
                return;
            Save(FullPath);
            if(Saved != null)
                Saved(this, EventArgs.Empty);
        }

        internal WfConnector FindConnector(Guid connectorId) {
            return Connectors.FirstOrDefault(c => c.Id == connectorId);
        }

        public void InitializeVisualData() {
            if(QueryNodeVisualData == null)
                return;
            foreach(var node in Nodes) {
                RaiseQueryVisualData(node);
            }
        }

        protected virtual void RaiseQueryVisualData(WfNode node) {
            if(QueryNodeVisualData != null)
                QueryNodeVisualData.Invoke(this, new WfNodeEventArgs(node));
        }

        public void Save(string fullPath) {
            FileName = Path.GetFileName(fullPath);
            if(string.IsNullOrEmpty(FileName))
                return;
            string path = Path.GetDirectoryName(fullPath);
            SerializationHelper.Save(this, GetType(), fullPath);
        }

        [XmlIgnore, Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public string FullPath { get; set; }

        public bool Load(string fileName) {
            Clear();
            FullPath = fileName;
            if(SerializationHelper.Load(this, GetType(), fileName)) {
                FileName = Path.GetFileName(fileName);
                if(Loaded != null)
                    Loaded(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public void Clear() {
            Nodes.Clear();
            Connectors.Clear();
            Name = string.Empty;
        }

        void ISupportSerialization.OnEndDeserialize() {
            foreach(var node in Nodes) {
                node.OwnerCollection = Nodes;
                node.OnEndDeserialize();
                RaiseQueryVisualData(node);
            }
            foreach(var conn in Connectors) {
                conn.Document = this;
                conn.To = FindConnectionPoint(conn.ToId);
                if(conn.To != null)
                    conn.To.Connectors.Add(conn);
                conn.From = FindConnectionPoint(conn.FromId);
                if(conn.From != null)
                    conn.From.Connectors.Add(conn);
            }
        }

        void ISupportSerialization.OnStartDeserialize() {
            Clear();
        }

        [Browsable(false)]
        public WfNodeCollection Nodes { get; private set; }

        [Browsable(false)]
        public List<WfConnector> Connectors { get; private set; }

        protected void OnPropertyChanged(string name) {
            if(this.propertyChanged != null)
                this.propertyChanged(this, new PropertyChangedEventArgs(name));
        }

        event PropertyChangedEventHandler propertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
            add {
                this.propertyChanged += value;
            }
            remove {
                this.propertyChanged -= value;
            }
        }

        [Browsable(false)]
        public List<WfColor> ItemColors { get; set; }

        protected virtual void InitializeDefaultColors() {
            ItemColors = new List<WfColor>();

            //ItemColors.Add(WfColor.FromArgb(nameof(ScriptActionMessage), 255, 45, 105, 5));
            //ItemColors.Add(WfColor.FromArgb(nameof(ScriptActionWait), 255, 239, 241, 245));
            //ItemColors.Add(WfColor.FromArgb(nameof(ScriptSetPropertyAction), 255, 255, 198, 0));
            //ItemColors.Add(WfColor.FromArgb(nameof(ScriptInvokeMethodAction), 255, 9, 94, 159));
            //ItemColors.Add(WfColor.FromArgb(nameof(ScriptActionActivateScript), 255, 218, 12, 12));
        }

        public WfConnectionPoint FindConnectionPoint(Guid pointId) {
            if(pointId == Guid.Empty)
                return null;
            foreach(var node in Nodes) {
                WfConnectionPoint pt = node.Inputs.FirstOrDefault(i => i.Id == pointId);
                if(pt != null)
                    return pt;
                pt = node.Outputs.FirstOrDefault(i => i.Id == pointId);
                if(pt != null)
                    return pt;
            }
            return null;
        }

        public List<Type> GetAvailableNodeTypes() {
            var res = GetType().GetCustomAttributes(typeof(XmlIncludeAttribute), true)
                .Where(a => ((XmlIncludeAttribute)a).Type.IsSubclassOf(typeof(WfNode)))
                .Select(a => ((XmlIncludeAttribute)a).Type)
                .ToList();
            return res;
        }
        public List<WfNode> GetAvailableToolbarItems() {
            var res = GetAvailableNodeTypes().Select(t => (WfNode)t.GetConstructor(new Type[] { }).Invoke(new object[] { })).ToList();
            return res;
        }

        public event WfNodeEventHandler QueryNodeVisualData;

        public void AddConnector(WfConnector c) {
            Connectors.Add(c);
            c.Document = this;
        }

        public virtual void Reset() {
            foreach(WfConnector connector in Connectors)
                connector.Reset();
            foreach(WfNode node in Nodes) {
                node.Reset();
            }
        }

        public void RemoveNode(WfNode node) {
            List<WfConnector> input = node.GetInputConnectors();
            List<WfConnector> output = node.GetOutputConnectors();
            Nodes.Remove(node);
            input.ForEach(i => i.Detach());
            output.ForEach(o => o.Detach());
        }
    }
}