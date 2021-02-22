/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CilTools.BytecodeAnalysis;

namespace CilView.UI.Controls
{
    class TextListViewer : FlowDocumentScrollViewer
    {
        List<Inline> items = new List<Inline>();
        FlowDocument doc = new FlowDocument();
        Inline selected = null;

        public TextListViewer()
        {
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            this.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            
            doc.TextAlignment = TextAlignment.Left;
            doc.MinPageWidth = 400;
            doc.PagePadding = new Thickness(0);
            this.Document = this.doc;
        }

        public void AddItem(Inline item)
        {
            Paragraph line = new Paragraph();
            line.Inlines.Add(item);
            items.Add(item);
            doc.Blocks.Add(line);
        }

        public int ItemCount { get { return this.items.Count; } }

        public Inline GetItem(int index)
        {
            if (index < 0) return null;
            if (index >= this.items.Count) return null;

            return this.items[index];
        }

        public void Clear()
        {
            UnselectItem();
            this.items.Clear();
            doc.Blocks.Clear();
        }

        void UnselectItem()
        {
            if (this.selected != null)
            {
                this.selected.FontWeight = FontWeights.Normal;
                this.selected = null;
            }
        }

        void SelectItem(Inline item)
        {
            item.FontWeight = FontWeights.Bold;
            this.selected = item;
        }

        public Inline SelectedItem
        {
            get { return this.selected; }

            set
            {
                UnselectItem();

                if (value == null) return;

                for (int i = 0; i < this.items.Count; i++)
                {
                    if(ReferenceEquals(value,this.items[i]))
                    {
                        SelectItem(this.items[i]);
                        break;
                    }
                }
            }
        }

        public int SelectedIndex
        {
            get
            {
                if (this.selected == null) return -1;

                for (int i = 0; i < this.items.Count; i++)
                {
                    if(ReferenceEquals(this.items[i],this.selected)) return i;
                }

                return -1;
            }

            set
            {
                UnselectItem();

                if (value < 0) return;
                if (value >= this.items.Count) return;

                SelectItem(this.items[value]);
            }
        }
         
    }
}
