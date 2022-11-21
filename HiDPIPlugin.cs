using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Diagnostics.Trace;
using SDRSharp.Common;
using SDRSharp.Radio;


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

        public void Initialize(ISharpControl controlInterface)
        {
#if DEBUG
            //Listeners.Add(new System.Diagnostics.TextWriterTraceListener("_HiDPIPlugin.log", "HiDPIListener"));
            //AutoFlush = true;
#endif
            TraceInformation("HiDPIPlugin()");
            _controlInterface = controlInterface;
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

        private object GetFieldValue(object obj, string name)
        {
            if (name == "" || obj == null) return null;
            return obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
        }

        private object GetPropertyValue(object obj, string name)
        {
            if (name == "" || obj == null) return null;
            return obj.GetType().GetProperty(name).GetValue(obj);
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

                var _MainForm = GetFieldValue(_controlInterface, "_owner");
                //TraceInformation($"MainForm: {ToStringOrNull(_MainForm)}");

                // Remove padding from MainForm
                var control = (Control)_MainForm;
                control.Padding = new Padding(4);

                // Reducing height of settingsTableLayoutPanel
                control = (Control)GetFieldValue(_MainForm, "settingsTableLayoutPanel");
                control.Height -= control.Height / 3;

                // Reducing width of fftRangeTableLayoutPanel
                var panel = (Panel)GetFieldValue(_MainForm, "fftRangeTableLayoutPanel");
                panel.AutoSize = false;
                panel.Width = 76;
                foreach (var _control in GetAllControls((Control)panel))
                    if (_control.Name.StartsWith("label"))
                        _control.Margin = new Padding(0);

                // Remove horizontal scrolling from left plugin list
                panel = (Panel)GetFieldValue(_MainForm, "scrollPanel");
                panel.Padding = new Padding(0);
                panel.Width -= 6;
                panel.AutoScroll = false;
                panel.HorizontalScroll.Enabled = false;
                panel.HorizontalScroll.Visible = false;
                panel.VerticalScroll.Enabled = true;
                panel.VerticalScroll.Visible = true;
                panel.AutoScroll = true;

                // Process CollapsiblePanels
                panel = (Panel)GetFieldValue(_MainForm, "controlPanel");
                foreach (var _collapsiblePanel in (IList)GetPropertyValue(panel, "Controls"))
                {
                    control = (Control)_collapsiblePanel;
                    var title = (string)GetPropertyValue(_collapsiblePanel, "PanelTitle");
                    //TraceInformation($"==> {ToStringOrNull(_collapsiblePanel)}:{control.Name}:{title}");

                    // Resize collapsible panels height from ResizePanelTitles_contentPanel list 
                    if (ResizePanelTitles_contentPanel.Contains(title))
                        foreach (var _control in GetAllControls((Control)control))
                            if (_control.Name == "contentPanel")
                                _control.Height -= _control.Height / 3 - 24;

                    // Resize collapsible panels height from ResizePanelTitles_reduce list 
                    if (ResizePanelTitles_reduce.Contains(title))
                        control.Height -= control.Height / 3;

                    // Resize collapsible panels height from ResizePanelTitles_increase list 
                    if (ResizePanelTitles_increase.Contains(title))
                        foreach (var _control in GetAllControls((Control)control))
                            if (_control.Name == "contentPanel")
                                _control.Height += _control.Height / 3 + 24;

                    // Hide collapsible panels from HidePanels list
                    if (HidePanelTitles.Contains(title))
                        foreach (var _control in GetAllControls((Control)_collapsiblePanel))
                            if (_control.Name == "titlePanel" || _control.Name == "contentPanel")
                                _control.Height = 0;

                    // Fixing Frequency column width
                    if (title == "Frequency Manager *")
                        foreach (var _control in GetAllControls((Control)_collapsiblePanel))
                            if (_control.Name == "frequencyDataGridView")
                                ((DataGridView)_control).Columns[1].Width = 144;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}");
            }
        }
    }
}
