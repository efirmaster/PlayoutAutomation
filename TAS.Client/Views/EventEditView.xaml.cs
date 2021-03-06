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
using System.Runtime.InteropServices;
using TAS.Server;
using System.Globalization;
using TAS.Common;
using TAS.Client.Common;

namespace TAS.Client
{
    public partial class EventEditView : UserControl
    {
        public EventEditView(RationalNumber frameRate)
        {
            InitializeComponent();
            ((TimeSpanToSMPTEConverter)Resources["TimeSpanToSMPTE"]).FrameRate = frameRate;
            ((DateTimeToSMPTEConverter)Resources["DateTimeToSMPTE"]).FrameRate = frameRate;
            ((TimeSpanToFramesConverter)Resources["TimeSpanToFrames"]).FrameRate = frameRate;
        }
    }
}

