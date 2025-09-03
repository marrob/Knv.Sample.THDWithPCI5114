using Ivi.Scope;
using NationalInstruments;
using NationalInstruments.DataInfrastructure;
using NationalInstruments.ModularInstruments.NIScope;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Knv.Sample.THDWithPCI5114
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
              new App();
        }



        class App
        {
            NIScope _scopeSession;
            readonly MainForm _mainForm;
            public App()
            {
                _mainForm = new MainForm();


                StartAcquisition(
                    resourceName: "PCI-5114-Scope",
                    channelName: "0",
                    verticalRange: 10.0,
                    verticalOffset: 0.0,
                    sampleRateMin: 4000,
                    recordLengthMin:4000,
                    triggerType: ScopeTriggerType.Edge);

                //--- Run ---
               // Application.Run(_mainForm);
            }

            void StartAcquisition(
                string resourceName, 
                string channelName,
                double verticalRange,
                double verticalOffset,
                double sampleRateMin,
                int recordLengthMin,
                ScopeTriggerType triggerType)
            {
                AnalogWaveformCollection<byte> byteWaveforms = null;
                ScopeWaveformInfo[] waveformInfo = null;
                try
                {
                    _scopeSession = new NIScope(resourceName, false, false);
                    //_scopeSession.DriverOperation.Warning += new EventHandler<ScopeWarningEventArgs>(DriverOperation_Warning);

                    // Configure the vertical parameters.
                    ScopeVerticalCoupling coupling = ScopeVerticalCoupling.DC;
                    double probeAttenuation = 1.0;
                    _scopeSession.Channels[channelName].Configure(verticalRange, verticalOffset, coupling, probeAttenuation, true);

                    // Configure the horizontal parameters.
                    double referencePosition = 50.0;
                    int numberOfRecords = 1;
                    bool enforceRealtime = true;
                    _scopeSession.Timing.ConfigureTiming(sampleRateMin, recordLengthMin, referencePosition, numberOfRecords, enforceRealtime);

                    // Configure the trigger.
                    if (triggerType == ScopeTriggerType.Edge)
                    {
                        ScopeTriggerSource source = ScopeTriggerSource.Channel0;
                        double level = 0.2;
                        ScopeTriggerSlope slope = ScopeTriggerSlope.Positive;
                        ScopeTriggerCoupling triggerCoupling = ScopeTriggerCoupling.DC;
                        PrecisionTimeSpan holdOff = PrecisionTimeSpan.Zero;
                        PrecisionTimeSpan delay = PrecisionTimeSpan.Zero;
                        _scopeSession.Trigger.EdgeTrigger.Configure(source, level, slope, triggerCoupling, holdOff, delay);
                    }
                    else
                    {
                        _scopeSession.Trigger.ConfigureTriggerImmediate();
                    }

                    PrecisionTimeSpan timeout = new PrecisionTimeSpan(5.0);
                    long recordLength = _scopeSession.Acquisition.RecordLength; 
                    _scopeSession.Measurement.Initiate();
                    byteWaveforms = _scopeSession.Channels[channelName].Measurement.FetchByte(timeout, recordLength, byteWaveforms, out waveformInfo);

                    PlotWaveforms(byteWaveforms);

                    StringBuilder offsetTextBuilder = new StringBuilder();
                    StringBuilder gainFactorTextBuilder = new StringBuilder();
                    for (int ii = 0; ii < waveformInfo.Length; ii++)
                    {
                        StringBuilder prefixTextBuilder = new StringBuilder();
                        if (ii > 0)
                        {
                            prefixTextBuilder.Append("; ");
                        }
                        prefixTextBuilder.Append("Waveform " + ii + ": ");
                        offsetTextBuilder.Append(prefixTextBuilder + waveformInfo[ii].Offset.ToString("E"));
                        gainFactorTextBuilder.Append(prefixTextBuilder + waveformInfo[ii].Gain.ToString("E"));
                    }
                }
                catch (Exception ex)
                {
                    //niScope Fetch WDT.vi:5210001<ERR>Maximum time exceeded before the operation completed.
                    //Status Code: -200284
                    if (ex.Message.Contains("-1074126845"))
                        MessageBox.Show("Maximum time exceeded before the operation completed");
                    else
                        ShowError(ex);
                }
                finally
                {
                    CloseSession();
                }
            }

            void PlotWaveforms<T>(AnalogWaveformCollection<T> waveforms)
            {
                int rowIndex;
                double[][] scaledRecords = new double[waveforms.Count][];
                for (int i = 0; i < waveforms.Count; i++)
                {
                    scaledRecords[i] = waveforms[i].GetScaledData();
                }

                string directory = "D:\\";
                string prefix = "Knv.Sample.THDWithPCI5114";
                var lines = new List<string>();

                for (rowIndex = 0; rowIndex < waveforms[0].SampleCount; rowIndex++)
                { 
                    foreach (AnalogWaveform<T> waveform in waveforms)
                    {
                        waveform.Samples[rowIndex].Value.ToString();
                    }
                    lines.Add($"{rowIndex};{waveforms[0].Samples[rowIndex].Value}");
                }


                if (!File.Exists(directory))
                    Directory.CreateDirectory(directory);

                var dt = DateTime.Now;
                var fileName = $"{prefix}_{dt:yyyy}{dt:MM}{dt:dd}_{dt:HH}{dt:mm}{dt:ss}.csv";
                using (var file = new StreamWriter($"{directory}\\{fileName}", true, Encoding.ASCII))
                    lines.ForEach(file.WriteLine);
            }

            void CloseSession()
            {
                try
                {
                    if (_scopeSession != null)
                    {
                        _scopeSession.Close();
                        _scopeSession = null;
                    }
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }

            void ShowError(Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
