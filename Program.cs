using Ivi.Scope;
using Knv.Sample.THDWithPCI5114.Data;
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
            const double SAMPLE_RATE_HZ = 50000;
            const int   SAMPLES_COUNT = 2000;
            public App()
            {
                _mainForm = new MainForm();
                StartAcquisition(
                    resourceName: "PCI-5114-Scope",
                    channelName: "0",
                    verticalRange: 10.0,
                    verticalOffset: 0.0,
                    sampleRateMin: SAMPLE_RATE_HZ,
                    recordLengthMin: SAMPLES_COUNT,
                    triggerType: ScopeTriggerType.Edge);

                //--- Run ---
                //Application.Run(_mainForm);
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

                    // --- Configure the vertical parameters ---
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

                /*
                // -128... -3, -2, -1, 0, 1, 2, 3 ... 127
                //
                // 127  |
                //      | 
                //   0  |----------------------------
                //      |
                //-128  |
                */

                string directory = "D:\\";
                string prefix = "Knv.Sample.THDWithPCI5114";
                var wfs = new WaveformStorage();
                wfs.Waveforms.Add(new Waveform() { Name = "1KHz Signal", DeltaX = 1.0 / SAMPLE_RATE_HZ  });
                wfs.Waveforms[0].YArray = new double[waveforms[0].SampleCount];

                for (rowIndex = 0; rowIndex < waveforms[0].SampleCount; rowIndex++)
                { 
                    var rawValue = waveforms[0].Samples[rowIndex].Value;
                    byte byteValue = Convert.ToByte(rawValue);
                    sbyte signedValue = unchecked((sbyte)byteValue);
                    wfs.Waveforms[0].YArray[rowIndex] = signedValue;
                }

                if (!File.Exists(directory))
                    Directory.CreateDirectory(directory);

                var dt = DateTime.Now;
                var fileName = $"{prefix}_{dt:yyyy}{dt:MM}{dt:dd}_{dt:HH}{dt:mm}{dt:ss}.csv";
                wfs.SaveToCsv($"{directory}\\{fileName}");
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
