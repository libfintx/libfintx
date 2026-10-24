using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace libfintx.FinTS
{
    public static partial class Helper
    {

        /// <summary>
        /// Fill given <code>TANDialog</code> and wait for user to enter a TAN.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dialogResult"></param>
        /// <param name="tanDialog"></param>
        public static async Task<string> WaitForTanAsync(FinTsClient client, HBCIDialogResult dialogResult, TANDialog tanDialog)
        {
            var BankCode_ = "HIRMS" + Parse_String(dialogResult.RawData, "'HIRMS", "'");
            string[] values = BankCode_.Split('+');
            foreach (var item in values)
            {
                if (!item.StartsWith("HIRMS"))
                    TransactionConsole.Output = item.Replace("::", ": ");
            }

            var HITAN = "HITAN" + Parse_String(dialogResult.RawData.Replace("?'", "").Replace("?:", ":").Replace("<br>", Environment.NewLine).Replace("?+", "??"), "'HITAN", "'");

            string HITANFlicker = string.Empty;

            var processes = client.BPD.HITANS.Where(h => h.Version == client.HITANS).SelectMany(t => t.TanProcesses);

            var processname = string.Empty;

            foreach (var item in processes)
            {
                if (item.TanCode == Convert.ToInt32(client.HIRMS))
                    processname = item.Name;
            }

            client.Logger.LogInformation($"Processing TAN process '{processname}' ...");

            // Smart-TAN plus optisch
            // chipTAN optisch
            if (processname.Equals("Smart-TAN plus optisch") || processname.Contains("chipTAN optisch"))
            {
                HITANFlicker = HITAN;
            }

            String[] values_ = HITAN.Split('+');

            int i = 1;

            foreach (var item in values_)
            {
                i = i + 1;

                if (i == 6)
                {
                    TransactionConsole.Output = TransactionConsole.Output + "??" + item.Replace("::", ": ").TrimStart();

                    TransactionConsole.Output = TransactionConsole.Output.Replace("??", " ")
                            .Replace("0030: ", "")
                            .Replace("1.", Environment.NewLine + "1.")
                            .Replace("2.", Environment.NewLine + "2.")
                            .Replace("3.", Environment.NewLine + "3.")
                            .Replace("4.", Environment.NewLine + "4.")
                            .Replace("5.", Environment.NewLine + "5.")
                            .Replace("6.", Environment.NewLine + "6.")
                            .Replace("7.", Environment.NewLine + "7.")
                            .Replace("8.", Environment.NewLine + "8.");
                }
            }

            // chipTAN optisch
            string chipTanFlicker = processname.Contains("chipTAN optisch") ? RenderChipTanFlickerCode(HITAN, client.Logger) : null;

            if (chipTanFlicker is not null)
            {
                flickerCodeRenderer = new FlickerRenderer(chipTanFlicker, tanDialog.PictureBox);
                if (!tanDialog.RenderFlickerCodeAsGif)
                {
                    RUN_flickerCodeRenderer();

                    Action action = STOP_flickerCodeRenderer;
                    TimeSpan span = new TimeSpan(0, 0, 0, 50);

                    ThreadStart start = delegate { RunAfterTimespan(action, span); };
                    Thread thread = new Thread(start);
                    thread.Start();
                }
                else
                {
#if USE_LIB_SixLabors_ImageSharp
                    tanDialog.FlickerImage = flickerCodeRenderer.RenderAsGif(tanDialog.FlickerWidth, tanDialog.FlickerHeight);
#else
                    throw new NotSupportedException($"{nameof(TANDialog)}.{nameof(TANDialog.RenderFlickerCodeAsGif)} is not supported.");
#endif

                }
            }

            // Smart-TAN plus optisch
            if (processname.Equals("Smart-TAN plus optisch"))
            {
                HITANFlicker = HITAN.Replace("?@", "??");

                string FlickerCode = string.Empty;

                String[] values__ = HITANFlicker.Split('@');

                int ii = 1;

                foreach (var item in values__)
                {
                    ii = ii + 1;

                    if (ii == 4)
                        FlickerCode = item;
                }

                FlickerCode flickerCode = new FlickerCode(FlickerCode.Trim());
                flickerCodeRenderer = new FlickerRenderer(flickerCode.Render(), tanDialog.PictureBox);
                if (!tanDialog.RenderFlickerCodeAsGif)
                {
                    RUN_flickerCodeRenderer();

                    Action action = STOP_flickerCodeRenderer;
                    TimeSpan span = new TimeSpan(0, 0, 0, 50);

                    ThreadStart start = delegate { RunAfterTimespan(action, span); };
                    Thread thread = new Thread(start);
                    thread.Start();
                }
                else
                {
#if USE_LIB_SixLabors_ImageSharp
                    tanDialog.FlickerImage = flickerCodeRenderer.RenderAsGif(tanDialog.FlickerWidth, tanDialog.FlickerHeight);
#else
                    throw new NotSupportedException($"{nameof(TANDialog)}.{nameof(TANDialog.RenderFlickerCodeAsGif)} is not supported.");
#endif
                }
            }

            // Smart-TAN photo
            if (processname.Equals("Smart-TAN photo"))
            {
                var PhotoCode = Parse_String(dialogResult.RawData, ".+@", "'HNSHA");

                var mCode = new MatrixCode(PhotoCode.Substring(5, PhotoCode.Length - 5));

                tanDialog.MatrixCode = mCode;
                mCode.Render(tanDialog.PictureBox);
            }

            // PhotoTAN
            if (processname.Equals("photoTAN-Verfahren") || processname.Equals("photoTAN"))
            {
                // HITAN:5:5:4+4++nmf3VmGQDT4qZ20190130091914641+Bitte geben Sie die photoTan ein+@3031@       image/pngÃŠÂ‰PNG
                var match = Regex.Match(dialogResult.RawData, @"HITAN.+@\d+@(.+)'(HNHBS|HNSHA)", RegexOptions.Singleline);
                if (match.Success)
                {
                    var PhotoBinary = match.Groups[1].Value;

                    var mCode = new MatrixCode(PhotoBinary);

                    tanDialog.MatrixCode = mCode;
                    mCode.Render(tanDialog.PictureBox);
                }
            }

            return await tanDialog.WaitForTanAsync();
        }

        /// <summary>
        /// Renders the chipTAN flicker code of a HITAN segment for the <see cref="FlickerRenderer"/>.
        /// Returns null when the segment carries no usable code; the user can then still enter a TAN
        /// from the challenge text (chipTAN manuell), so a missing or malformed code never ends the dialog.
        /// </summary>
        internal static string RenderChipTanFlickerCode(string hitan, ILogger logger)
        {
            var code = ExtractChipTanFlickerCode(hitan);
            if (code is null)
            {
                logger?.LogWarning("HITAN carries no flicker code for chipTAN optisch, showing the challenge text only.");
                return null;
            }

            try
            {
                return new FlickerCode(code).Render();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "The chipTAN flicker code could not be read, showing the challenge text only.");
                return null;
            }
        }

        /// <summary>
        /// Extracts the chipTAN flicker code from a HITAN segment. Banks send it either inside the challenge
        /// text between CHLGUC and CHLGTEXT, or in the binary data element "Challenge HHD_UC" (@length@data).
        /// Returns null when the segment carries neither.
        /// </summary>
        internal static string ExtractChipTanFlickerCode(string hitan)
        {
            if (string.IsNullOrEmpty(hitan))
                return null;

            // CHLGUC is followed by a four-digit length, then the code
            var text = Parse_String(hitan, "CHLGUC", "CHLGTEXT");
            if (text.Replace(" ", "").Length > 4)
                return "CHLGUC" + text + "CHLGTEXT";

            var match = Regex.Match(hitan, @"\+@(\d+)@");
            if (!match.Success)
                return null;

            int length = int.Parse(match.Groups[1].Value);
            int start = match.Index + match.Length;
            if (length == 0 || start + length > hitan.Length)
                return null;

            return hitan.Substring(start, length);
        }
    }
}
