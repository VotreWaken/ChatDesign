﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChatDesign.Control
{
    public sealed class MyScrollViewer : ScrollViewer
    {
        public static readonly DependencyProperty IsInViewportProperty =
            DependencyProperty.RegisterAttached("IsInViewport", typeof(bool), typeof(MyScrollViewer));

        public static bool GetIsInViewport(UIElement element)
        {
            return (bool)element.GetValue(IsInViewportProperty);
        }

        public static void SetIsInViewport(UIElement element, bool value)
        {
            element.SetValue(IsInViewportProperty, value);
        }

        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            base.OnScrollChanged(e);

            var panel = Content as Panel;
            if (panel == null)
            {
                return;
            }

            Rect viewport = new Rect(new Point(0, 0), RenderSize);

            foreach (UIElement child in panel.Children)
            {
                if (!child.IsVisible)
                {
                    SetIsInViewport(child, false);
                    continue;
                }

                GeneralTransform transform = child.TransformToAncestor(this);
                Rect childBounds = transform.TransformBounds(new Rect(new Point(0, 0), child.RenderSize));
                SetIsInViewport(child, viewport.IntersectsWith(childBounds));
            }
        }
    }
}
