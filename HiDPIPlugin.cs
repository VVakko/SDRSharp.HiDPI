using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using static System.Diagnostics.Trace;
using SDRSharp.Common;
using SDRSharp.Radio;
using System.Collections;
using System.Drawing;
using System.Reflection;

namespace SDRSharp.HiDPI
{
    public class HiDPIPlugin : ISharpPlugin
    {
        #region ISharpPlugin Members

        private const string _displayName = "HiDPI";
        private ISharpControl _controlInterface;
        private UserControl _userControl;
        private bool _isHiDPIPatched = false;

        #endregion

        #region ISharpPlugin implementation

        public UserControl Gui
        {
            get { return null; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public void Close()
        {
            _userControl?.Dispose();
        }

        public void Initialize(ISharpControl control)
        {
#if DEBUG
            //Listeners.Add(new System.Diagnostics.TextWriterTraceListener("_HiDPIPlugin.log", "HiDPIListener"));
            //AutoFlush = true;
#endif
            TraceInformation("HiDPIPlugin()");
            _controlInterface = control;
            _userControl = new UserControl();
            _userControl.VisibleChanged += UserControl_VisibleChanged;
            _controlInterface.RegisterFrontControl(_userControl, PluginPosition.Top);
            if (_userControl.Visible) _userControl.Hide();
        }

        #endregion


        private void UserControl_VisibleChanged(object sender, EventArgs e)
        {
            if (_isHiDPIPatched) return;

            _isHiDPIPatched = true;
            Graphics gp = Graphics.FromHwnd(IntPtr.Zero);
            TraceInformation($"InitializeHiDPIPatch({gp.DpiX})");
            if (gp.DpiX == 192) // 200% scaling
            {
                HiDPIPatch();
            }
            else
            {
                TraceInformation($"HiDPI patcher works only in 200% scaling");
            }
        }

        private IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control control in root.Controls)
                foreach (Control child in GetAllControls(control).Where(c => c != null))
                    yield return child;
            yield return root;
        }

        private string ToStringOrNull(object value)
        {
            return value != null ? value.ToString() : "null";
        }

        private void HiDPIPatch()
        {
            try
            {
                var ResizePanelTitles_reduce = new string[] {
                    "Radio", "Audio", "FFT Display", "AGC", "Band Plan *",
                    "Baseband Noise Blanker *", "Demodulator Noise Blanker *"
                };
                var ResizePanelTitles_increase = new string[] {
                    "Zoom FFT *"
                };
                var ResizePanelTitles_contentPanel = new string[] {
                    "Band Plan *", "Baseband Noise Blanker *", "Demodulator Noise Blanker *"
                };
                var HidePanelTitles = new string[] {
                    //"Audio Noise Reduction *", "IF Noise Reduction *",
                    //"Baseband Noise Blanker *", "Demodulator Noise Blanker *"
                };
                var _MainForm = _controlInterface.GetType().GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_controlInterface);
                //TraceInformation($"___ MainForm: {ToStringOrNull(_MainForm)}");

                // Remove padding from MainForm
                var control = (Control)_MainForm;
                control.Padding = new Padding(4);

                // Reducing height of settingsTableLayoutPanel
                control = (Control)_MainForm.GetType().GetField("settingsTableLayoutPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_MainForm);
                control.Height -= control.Height / 3;

                // Reducing width of fftRangeTableLayoutPanel
                var panel = (Panel)_MainForm.GetType().GetField("fftRangeTableLayoutPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_MainForm);
                panel.AutoSize = false;
                panel.Width = 76;
                foreach (var _control in GetAllControls((Control)panel))
                    if (_control.Name.StartsWith("label"))
                        _control.Margin = new Padding(0);
                        //_control.Height -= _control.Height / 3 - 24;

                // Remove horizontal scrolling from left plugin list
                panel = (Panel)_MainForm.GetType().GetField("scrollPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_MainForm);
                panel.Padding = new Padding(0);
                panel.Width -= 6;
                panel.AutoScroll = false;
                panel.HorizontalScroll.Enabled = false;
                panel.HorizontalScroll.Visible = false;
                panel.VerticalScroll.Enabled = true;
                panel.VerticalScroll.Visible = true;
                panel.AutoScroll = true;

                // Process CollapsiblePanels
                panel = (Panel)_MainForm.GetType().GetField("controlPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_MainForm);
                var _controls = (IList)panel.GetType().GetProperty("Controls").GetValue(panel);
                //TraceInformation($"_controls: {ToStringOrNull(_controls)}");
                foreach (var _collapsiblePanel in _controls)
                {
                    control = (Control)_collapsiblePanel;
                    var _panelTitle = (string)_collapsiblePanel.GetType().GetProperty("PanelTitle").GetValue(_collapsiblePanel);
                    //TraceInformation($"==> {ToStringOrNull(_collapsiblePanel)}:{control.Name}:{_panelTitle}");
                    if (ResizePanelTitles_contentPanel.Contains(_panelTitle))
                    {
                        // Resize collapsible panels height from ResizePanelTitles_contentPanel list 
                        foreach (var _control in GetAllControls((Control)control))
                            if (_control.Name == "contentPanel")
                                _control.Height -= _control.Height / 3 - 24;
                    }
                    if (ResizePanelTitles_reduce.Contains(_panelTitle))
                    {
                        // Resize collapsible panels height from ResizePanelTitles_reduce list 
                        control.Height -= control.Height / 3;
                    }
                    if (ResizePanelTitles_increase.Contains(_panelTitle))
                    {
                        // Resize collapsible panels height from ResizePanelTitles_increase list 
                        foreach (var _control in GetAllControls((Control)control))
                            if (_control.Name == "contentPanel")
                                _control.Height += _control.Height / 3 + 24;
                    }
                    if (HidePanelTitles.Contains(_panelTitle))
                    {
                        // Hide collapsible panels from HidePanels list
                        foreach (var _control in GetAllControls((Control)_collapsiblePanel))
                            if (_control.Name == "titlePanel" || _control.Name == "contentPanel")
                                _control.Height = 0;
                    }
                    if (_panelTitle == "Frequency Manager *")
                    {
                        // Fixing Frequency column width
                        foreach (var _control in GetAllControls((Control)_collapsiblePanel))
                            if (_control.Name == "frequencyDataGridView")
                                ((DataGridView)_control).Columns[1].Width = 144;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}");
            }
        }
    }
}
