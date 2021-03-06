﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TAS.Client.Common;
using TAS.Common;

namespace TAS.Client.Views
{
    /// <summary>
    /// Interaction logic for CommandScriptItemEditView.xaml
    /// </summary>
    public partial class CommandScriptItemEditView : UserControl
    {
        public CommandScriptItemEditView(RationalNumber frameRate)
        {
            InitializeComponent();
            ((TimeSpanToSMPTEConverter)Resources["TimeSpanToSMPTE"]).FrameRate = frameRate;
        }
    }
}
