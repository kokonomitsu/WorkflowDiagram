﻿using DevExpress.Diagram.Core;
using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.Utils;
using DevExpress.Utils.DPI;
using DevExpress.Utils.Drawing;
using DevExpress.Utils.Html;
using DevExpress.Utils.Html.Dom;
using DevExpress.Utils.Html.Internal;
using DevExpress.XtraDiagram;
using DevExpress.XtraDiagram.Base;
using DevExpress.XtraDiagram.ViewInfo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WorkflowDiagram.UI.Win {
    public class NodeItem : DiagramItem, IDxHtmlClient {
        public NodeItem(WfNode node, CustomDiagramControl diagramControl) {
            Diagram = diagramControl;
            Node = node;
            SubscribeEvents();
            DataContext = node;
            Position = new PointFloat(Node.X, Node.Y);
            Width = Node.Width;
            CanRotate = false;

            HtmlTemplate = CreateHtmlTemplate();
            TemplateElement = CreateHtmlTemplateElement(CreateHtmlTemplate(HtmlTemplate), this);
            System.Windows.Size sz = CalcHtmlTemplateSize(new System.Windows.Size(Node.Width, Node.Height));
            Height = (float)sz.Height;
            Node.Height = (float)sz.Height;
            ConnectionPoints = GetConnectionPoints();
        }

        protected virtual void SubscribeEvents() {
            INotifyPropertyChanged notify = Node as INotifyPropertyChanged;
            if(notify != null)
                notify.PropertyChanged += OnNodePropertyChanged;
        }

        protected virtual void UnsubscribeEvents() {
            INotifyPropertyChanged notify = Node as INotifyPropertyChanged;
            if(notify != null)
                notify.PropertyChanged -= OnNodePropertyChanged;
        }

        private void OnNodePropertyChanged(object sender, PropertyChangedEventArgs e) {
            TemplateInfoCalculated = false;
            InvalidateVisual();
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            UnsubscribeEvents();
        }

        public CustomDiagramControl Diagram { get; private set; }

        protected override IDiagramItemDecorator CreateDecorator() {
            return new NodeItemDecorator(this);
        }

        protected override DiagramItemController CreateItemController() {
            return new NodeDiagramItemController(this);
        }

        protected override IDiagramItemView CreateView(DiagramControlViewInfo viewInfo, DiagramAppearanceObject appearance) {
            return new NodeDiagramItemView(this);
        }

        public WfNode Node { get; private set; }

        protected override void OnBoundsChanged(DiagramItemBoundsChangedEventArgs e) {
            base.OnBoundsChanged(e);
            Node.X = e.NewPosition.X;
            Node.Y = e.NewPosition.Y;
            Node.Width = e.NewSize.Width;
            Node.Height = e.NewSize.Height;
            
            if(e.OldSize != e.NewSize)
                UpdateConnectionPoints();
        }

        protected virtual PointCollection GetConnectionPoints() {
            Size size = TemplateElement.ViewInfo.Size;
            List<PointFloat> l = new List<PointFloat>();
            foreach(var point in Node.Points) {
                var element = TemplateElement.FindElementById(point.Name);
                Rectangle r = element.ViewInfo.AbsoluteBounds;
                PointFloat ptf = new PointFloat((r.X + r.Width / 2.0f) / size.Width, (r.Y + r.Height / 2.0f) / size.Height);
                l.Add(ptf);
            }
            return new PointCollection(l);
        }

        protected internal NodeDiagramItemView View { get; internal set; }

        protected internal virtual void UpdateConnectionPoints() {
            CalcHtmlTemplateSize(new System.Windows.Size(Width, Height));
            ConnectionPoints = GetConnectionPoints();
        }

        protected virtual HtmlTemplate CreateHtmlTemplate() {
            var templates = Diagram.HtmlTemplates;
            var defaultTemplate = GetDefaultTemplate();
            if(defaultTemplate == null)
                return null;
            var inRow = templates.FirstOrDefault(t => t.Name == "InRow");
            var outRow = templates.FirstOrDefault(t => t.Name == "OutRow");

            var res = defaultTemplate.Clone();

            StringBuilder b = new StringBuilder();
            foreach(var point in Node.Inputs) {
                b.AppendLine(string.Format(inRow.Template, point.Name));
            }
            res.Template = res.Template.Replace("InRows", b.ToString());
            b.Clear();
            foreach(var point in Node.Outputs) {
                b.AppendLine(string.Format(outRow.Template, point.Name));
            }
            res.Template = res.Template.Replace("OutRows", b.ToString());
            res.Styles += "\n" + inRow.Styles + "\n" + outRow.Styles;
            return res;
        }

        protected virtual HtmlTemplate GetDefaultTemplate() {
            var templates = Diagram.HtmlTemplates;
            var res = templates.FirstOrDefault(t => t.Name == Node.VisualTemplateName);
            if(res == null)
                res = templates.FirstOrDefault(t => t.Name == "Default");
            return res;
        }

        protected internal bool TemplateInfoCalculated { get; set; }
        public HtmlTemplate HtmlTemplate { get; protected set; }
        public DxHtmlRootElement TemplateElement { get; protected set; }

        DxHtmlRootElement IDxHtmlClient.Element => TemplateElement;

        protected internal DxHtmlDocumentRootNode CreateHtmlTemplate(HtmlTemplate template) {
            CssParser parser = new CssParser();
            CssStyleSheet styles = parser.Parse(template.Styles);
            return DxHtmlParser.Default.Parse(template.Template, styles);
        }

        protected internal virtual DxHtmlRootElement CreateHtmlTemplateElement(DxHtmlDocumentRootNode element, IDxHtmlClient itemInfo) {
            return Diagram.HtmlContext.Get(element, itemInfo);
        }

        protected internal virtual Size CalculateHtmlTemplate(GraphicsCache cache, Rectangle rect) {
            CssDynamicState state = TemplateElement.DynamicState;

            bool hover = state.HasFlag(DevExpress.Utils.Html.Internal.CssDynamicState.Hover);
            bool active = state.HasFlag(DevExpress.Utils.Html.Internal.CssDynamicState.Active);
            bool enabled = !state.HasFlag(DevExpress.Utils.Html.Internal.CssDynamicState.Disabled);
            bool selected = state.HasFlag(DevExpress.Utils.Html.Internal.CssDynamicState.Select);

            if(selected != IsSelected) {
                TemplateElement.Select(IsSelected);
                TemplateInfoCalculated = false;
            }
            //if(State.HasFlag(ObjectState.Hot) != hover) {
            //    TemplateElement.Focus(hover);
            //    TemplateInfoCalculated = false;
            //}

            AppearanceObject app = Appearance;
            if(TemplateInfoCalculated &&
                TemplateElement.ViewInfo.Font == app.GetFont() &&
                TemplateElement.ViewInfo.ForeColor == app.GetForeColor() &&
                TemplateElement.Size.Width == rect.Width) {
                TemplateElement.Location = rect.Location;
                return TemplateElement.RootElement.Size;
            }


            using(GraphicsCacheDxHtmlWrapper wrapper = new GraphicsCacheDxHtmlWrapper(cache, UserLookAndFeel.Default)) {
                var elemVisit = TemplateElement.FindElementById("key_visit");
                var elemInit = TemplateElement.FindElementById("key_init");

                elemInit.Style.SetBackgroundColor(Node.IsInitialized ? Color.FromArgb(255, Color.Green) : Color.FromArgb(40, Color.Green));
                elemVisit.Style.SetBackgroundColor(Color.FromArgb(40, Color.OrangeRed));

                TemplateElement.Calc(wrapper, rect, app.GetFont(), app.GetForeColor());
            }
            TemplateInfoCalculated = true;
            if(TemplateElement.RootElement.Size.Height != Height) {

            }
            return TemplateElement.RootElement.Size;
        }

        internal void CheckUpdateHtmlTemplate(GraphicsCache cache, Rectangle r) {
            ScaleHelper prev = cache.ScaleDPI;
            cache.ScaleDPI = ScaleHelper.NoScale;
            try {
                if(!TemplateInfoCalculated || TemplateElement.ViewInfo.Bounds.Size != r.Size) {
                    Size sz = CalculateHtmlTemplate(cache, r);
                    if(sz.Width != Width || sz.Height != Height) {
                        Width = sz.Width;
                        Height = sz.Height;
                    }

                }
                else
                    TemplateElement.ViewInfo.Location = r.Location;
            }
            finally {
                cache.ScaleDPI = prev;
            }
        }

        object IDxHtmlClient.GetImage(string imageId, bool field, DxHtmlElementBase element) {
            return GetHtmlImageCore(imageId, field);
        }

        string IDxHtmlClient.GetDisplayValue(string fieldName, DxHtmlElementBase element) {
            return GetHtmlDisplayValueCore(fieldName);
        }

        object IDxHtmlClient.GetValue(string fieldName, DxHtmlElementBase element) {
            return GetHtmlValueCore(fieldName);
        }

        protected internal object GetHtmlImageCore(string imageId, bool field) {
            if(!field)
                return ImageCollection.GetImageListImage(Diagram.HtmlImages, imageId);

            PropertyInfo pi = null;
            if(Node != null) {
                pi = Node.GetType().GetProperty(imageId, BindingFlags.Instance | BindingFlags.Public);
                if(pi != null)
                    return pi.GetValue(Node);
            }
            return null;
        }

        protected internal string GetHtmlDisplayValueCore(string fieldName) {
            PropertyInfo pi = null;
            if(Node != null) {
                pi = Node.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if(pi != null)
                    return pi.GetValue(Node)?.ToString();
            }

            pi = this.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            return pi == null ? null : pi.GetValue(this)?.ToString();
        }

        protected internal object GetHtmlValueCore(string fieldName) {
            PropertyInfo pi = null;
            if(Node != null) {
                pi = Node.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if(pi != null)
                    return pi.GetValue(Node);
            }

            pi = this.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            return pi == null ? null : pi.GetValue(this);
        }

        internal System.Windows.Size CalcHtmlTemplateSize(System.Windows.Size sz) {
            AppearanceObject app = Appearance;
            if(TemplateInfoCalculated &&
                TemplateElement.ViewInfo.Font == app.GetFont() &&
                TemplateElement.ViewInfo.ForeColor == app.GetForeColor() &&
                TemplateElement.Size.Width == (int)sz.Width) {
                Size s = TemplateElement.RootElement.Size;
                return new System.Windows.Size(s.Width, s.Height);
            }

            CustomDiagramControl d = Diagram;
            if(d == null)
                return System.Windows.Size.Empty;
            if(d.IsDisposed)
                return System.Windows.Size.Empty;
            
            Size res = System.Drawing.Size.Empty;
            GraphicsCache cache = d.CreateGraphicsCache();
            try {
                res = CalculateHtmlTemplate(cache, new Rectangle(0, 0, (int)sz.Width, 0));
            }
            finally {
                d.ReleaseCache(cache);
            }
            return new System.Windows.Size(res.Width, Math.Max(10, res.Height));
        }
    }

    public class NodeDiagramItemController : DiagramItemController {
        public NodeDiagramItemController(IDiagramItem item) : base(item) { 
        }
        protected override void UpdateLayout() {
            base.UpdateLayout();
        }
    }

    public class NodeDiagramItemView : IDiagramItemView {
        public NodeDiagramItemView(NodeItem item) {
            Node = item;
            Node.View = this;
            
        }
        

        public NodeItem Node { get; private set; }

        public void Dispose() {

        }

        public void Draw(GraphicsCache cache, DiagramItemDrawArgs args) {
            Rectangle r = new Rectangle(0, 0, (int)Node.Bounds.Width, (int)Node.Bounds.Height);
            Node.CheckUpdateHtmlTemplate(cache, r);
            using(GraphicsCacheDxHtmlWrapper wr = new GraphicsCacheDxHtmlWrapper(cache, UserLookAndFeel.Default)) {
                Node.TemplateElement.Draw(wr);
            }
        }

        public bool HitTest(PointF pt) {
            return true;
        }
    }

    public class NodeItemDecorator : IDiagramItemDecorator {
        public NodeItemDecorator(NodeItem node) {
            Node = node;
        }

        public NodeItem Node { get; private set; }
        public void Draw(GraphicsCache cache, DiagramAppearanceObject appearance, Func<Color, Color> fadeOutAnimation, IDiagramItemView itemView, SizeF size, float zoomFactor, CustomDrawItemMode drawMode, DiagramDrawingContext context) {
            itemView.Draw(cache, new DiagramItemDrawArgs(null, null, drawMode, context));
        }
    }
}