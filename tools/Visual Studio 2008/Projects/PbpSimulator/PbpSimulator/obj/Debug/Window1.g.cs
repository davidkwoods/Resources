﻿#pragma checksum "..\..\Window1.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "03546498EB1C4E374CE4683F45D247EA"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4927
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace PbpSimulator {
    
    
    /// <summary>
    /// Window1
    /// </summary>
    public partial class Window1 : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 20 "..\..\Window1.xaml"
        internal System.Windows.Controls.TextBox InputFileBox;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\Window1.xaml"
        internal System.Windows.Controls.Button FileButton;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\Window1.xaml"
        internal System.Windows.Controls.Button ClearButton;
        
        #line default
        #line hidden
        
        
        #line 26 "..\..\Window1.xaml"
        internal System.Windows.Controls.TextBox OutputFileBox;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\Window1.xaml"
        internal System.Windows.Controls.Button OutputButton;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\Window1.xaml"
        internal System.Windows.Controls.Button StartButton;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\Window1.xaml"
        internal System.Windows.Controls.TextBox ResultsBox;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\Window1.xaml"
        internal System.Windows.Controls.ProgressBar Progress;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/PbpSimulator;component/window1.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\Window1.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 5 "..\..\Window1.xaml"
            ((PbpSimulator.Window1)(target)).Loaded += new System.Windows.RoutedEventHandler(this.Window_Loaded);
            
            #line default
            #line hidden
            return;
            case 2:
            this.InputFileBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this.FileButton = ((System.Windows.Controls.Button)(target));
            
            #line 21 "..\..\Window1.xaml"
            this.FileButton.Click += new System.Windows.RoutedEventHandler(this.FileButton_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.ClearButton = ((System.Windows.Controls.Button)(target));
            
            #line 23 "..\..\Window1.xaml"
            this.ClearButton.Click += new System.Windows.RoutedEventHandler(this.ClearButton_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.OutputFileBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 6:
            this.OutputButton = ((System.Windows.Controls.Button)(target));
            
            #line 27 "..\..\Window1.xaml"
            this.OutputButton.Click += new System.Windows.RoutedEventHandler(this.OutputButton_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.StartButton = ((System.Windows.Controls.Button)(target));
            
            #line 29 "..\..\Window1.xaml"
            this.StartButton.Click += new System.Windows.RoutedEventHandler(this.StartButton_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.ResultsBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 9:
            this.Progress = ((System.Windows.Controls.ProgressBar)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}