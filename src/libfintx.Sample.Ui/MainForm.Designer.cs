namespace libfintx.Sample.Ui
{
    partial class MainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            txt_hbci_meldung = new System.Windows.Forms.TextBox();
            btn_auftrag_bestätigen_tan = new System.Windows.Forms.Button();
            btn_überweisen = new System.Windows.Forms.Button();
            camt_053_abholen = new System.Windows.Forms.Button();
            camt_052_abholen = new System.Windows.Forms.Button();
            btn_umsätze_abholen = new System.Windows.Forms.Button();
            btn_kontostand_abfragen = new System.Windows.Forms.Button();
            btn_synchronisation = new System.Windows.Forms.Button();
            pBox_tan = new System.Windows.Forms.PictureBox();
            txt_tanverfahren = new System.Windows.Forms.TextBox();
            lbl_tanverfahren = new System.Windows.Forms.Label();
            txt_verwendungszweck = new System.Windows.Forms.TextBox();
            lbl_verwendungszweck = new System.Windows.Forms.Label();
            txt_betrag = new System.Windows.Forms.TextBox();
            lbl_betrag = new System.Windows.Forms.Label();
            txt_empfängerbic = new System.Windows.Forms.TextBox();
            lbl_empfängerbic = new System.Windows.Forms.Label();
            txt_empfängeriban = new System.Windows.Forms.TextBox();
            lbl_empfängeriban = new System.Windows.Forms.Label();
            txt_empfängername = new System.Windows.Forms.TextBox();
            lbl_empfängername = new System.Windows.Forms.Label();
            txt_pin = new System.Windows.Forms.TextBox();
            lbl_pin = new System.Windows.Forms.Label();
            txt_userid = new System.Windows.Forms.TextBox();
            lbl_userid = new System.Windows.Forms.Label();
            txt_hbci_version = new System.Windows.Forms.TextBox();
            lbl_hbci_version = new System.Windows.Forms.Label();
            txt_url = new System.Windows.Forms.TextBox();
            lbl_url = new System.Windows.Forms.Label();
            txt_iban = new System.Windows.Forms.TextBox();
            lbl_iban = new System.Windows.Forms.Label();
            txt_bic = new System.Windows.Forms.TextBox();
            lbl_bic = new System.Windows.Forms.Label();
            txt_bankleitzahl = new System.Windows.Forms.TextBox();
            lbl_bankleitzahl = new System.Windows.Forms.Label();
            txt_kontonummer = new System.Windows.Forms.TextBox();
            lbl_kontonummer = new System.Windows.Forms.Label();
            btn_zugelassene_tanverfahren = new System.Windows.Forms.Button();
            btn_bankdaten_laden = new System.Windows.Forms.Button();
            btn_überweisungsdaten_laden = new System.Windows.Forms.Button();
            txt_tan = new System.Windows.Forms.TextBox();
            lbl_tan = new System.Windows.Forms.Label();
            btn_konten_anzeigen = new System.Windows.Forms.Button();
            btn_tan_medium_name_abfragen = new System.Windows.Forms.Button();
            chk_tracing = new System.Windows.Forms.CheckBox();
            chk_tracingFormatted = new System.Windows.Forms.CheckBox();
            lbl_bankleitzahl_zentrale = new System.Windows.Forms.Label();
            txt_bankleitzahl_zentrale = new System.Windows.Forms.TextBox();
            txt_tan_medium = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            date_umsatzabruf_von = new System.Windows.Forms.DateTimePicker();
            chk_umsatzabruf_von = new System.Windows.Forms.CheckBox();
            chk_umsatzabruf_bis = new System.Windows.Forms.CheckBox();
            date_umsatzabruf_bis = new System.Windows.Forms.DateTimePicker();
            label3 = new System.Windows.Forms.Label();
            btn_daueraufträge_abholen = new System.Windows.Forms.Button();
            btn_terminueberweisungen_abholen = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize) pBox_tan).BeginInit();
            SuspendLayout();
            // 
            // txt_hbci_meldung
            // 
            txt_hbci_meldung.Anchor =  System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txt_hbci_meldung.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            txt_hbci_meldung.ForeColor = System.Drawing.SystemColors.Window;
            txt_hbci_meldung.Location = new System.Drawing.Point(0, 698);
            txt_hbci_meldung.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_hbci_meldung.Multiline = true;
            txt_hbci_meldung.Name = "txt_hbci_meldung";
            txt_hbci_meldung.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txt_hbci_meldung.Size = new System.Drawing.Size(2032, 758);
            txt_hbci_meldung.TabIndex = 88;
            // 
            // btn_auftrag_bestätigen_tan
            // 
            btn_auftrag_bestätigen_tan.Location = new System.Drawing.Point(1376, 525);
            btn_auftrag_bestätigen_tan.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_auftrag_bestätigen_tan.Name = "btn_auftrag_bestätigen_tan";
            btn_auftrag_bestätigen_tan.Size = new System.Drawing.Size(196, 75);
            btn_auftrag_bestätigen_tan.TabIndex = 81;
            btn_auftrag_bestätigen_tan.Text = "Mit TAN bestätigen";
            btn_auftrag_bestätigen_tan.UseVisualStyleBackColor = true;
            btn_auftrag_bestätigen_tan.Click += btn_auftrag_bestätigen_tan_Click;
            // 
            // btn_überweisen
            // 
            btn_überweisen.Location = new System.Drawing.Point(1234, 525);
            btn_überweisen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_überweisen.Name = "btn_überweisen";
            btn_überweisen.Size = new System.Drawing.Size(134, 75);
            btn_überweisen.TabIndex = 80;
            btn_überweisen.Text = "Überweisen";
            btn_überweisen.UseVisualStyleBackColor = true;
            btn_überweisen.Click += btn_überweisen_Click;
            // 
            // camt_053_abholen
            // 
            camt_053_abholen.Location = new System.Drawing.Point(814, 525);
            camt_053_abholen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            camt_053_abholen.Name = "camt_053_abholen";
            camt_053_abholen.Size = new System.Drawing.Size(190, 75);
            camt_053_abholen.TabIndex = 79;
            camt_053_abholen.Text = "camt053 abholen";
            camt_053_abholen.UseVisualStyleBackColor = true;
            camt_053_abholen.Click += camt_053_abholen_Click;
            // 
            // camt_052_abholen
            // 
            camt_052_abholen.Location = new System.Drawing.Point(606, 525);
            camt_052_abholen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            camt_052_abholen.Name = "camt_052_abholen";
            camt_052_abholen.Size = new System.Drawing.Size(196, 75);
            camt_052_abholen.TabIndex = 78;
            camt_052_abholen.Text = "camt052 abholen";
            camt_052_abholen.UseVisualStyleBackColor = true;
            camt_052_abholen.Click += camt_052_abholen_Click;
            // 
            // btn_umsätze_abholen
            // 
            btn_umsätze_abholen.Location = new System.Drawing.Point(432, 525);
            btn_umsätze_abholen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_umsätze_abholen.Name = "btn_umsätze_abholen";
            btn_umsätze_abholen.Size = new System.Drawing.Size(165, 75);
            btn_umsätze_abholen.TabIndex = 77;
            btn_umsätze_abholen.Text = "Umsätze abholen";
            btn_umsätze_abholen.UseVisualStyleBackColor = true;
            btn_umsätze_abholen.Click += btn_umsätze_abholen_Click;
            // 
            // btn_kontostand_abfragen
            // 
            btn_kontostand_abfragen.Location = new System.Drawing.Point(222, 525);
            btn_kontostand_abfragen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_kontostand_abfragen.Name = "btn_kontostand_abfragen";
            btn_kontostand_abfragen.Size = new System.Drawing.Size(200, 75);
            btn_kontostand_abfragen.TabIndex = 76;
            btn_kontostand_abfragen.Text = "Kontostand abfragen";
            btn_kontostand_abfragen.UseVisualStyleBackColor = true;
            btn_kontostand_abfragen.Click += btn_kontostand_abfragen_Click;
            // 
            // btn_synchronisation
            // 
            btn_synchronisation.Location = new System.Drawing.Point(25, 525);
            btn_synchronisation.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_synchronisation.Name = "btn_synchronisation";
            btn_synchronisation.Size = new System.Drawing.Size(186, 75);
            btn_synchronisation.TabIndex = 75;
            btn_synchronisation.Text = "Synchronisation";
            btn_synchronisation.UseVisualStyleBackColor = true;
            btn_synchronisation.Click += btn_synchronisation_Click;
            // 
            // pBox_tan
            // 
            pBox_tan.ErrorImage = libfintx_test.Properties.Resources.tan;
            pBox_tan.Image = libfintx_test.Properties.Resources.tan;
            pBox_tan.InitialImage = libfintx_test.Properties.Resources.tan;
            pBox_tan.Location = new System.Drawing.Point(1535, 11);
            pBox_tan.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            pBox_tan.Name = "pBox_tan";
            pBox_tan.Size = new System.Drawing.Size(416, 384);
            pBox_tan.TabIndex = 73;
            pBox_tan.TabStop = false;
            // 
            // txt_tanverfahren
            // 
            txt_tanverfahren.Location = new System.Drawing.Point(985, 261);
            txt_tanverfahren.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_tanverfahren.Name = "txt_tanverfahren";
            txt_tanverfahren.Size = new System.Drawing.Size(492, 31);
            txt_tanverfahren.TabIndex = 72;
            // 
            // lbl_tanverfahren
            // 
            lbl_tanverfahren.AutoSize = true;
            lbl_tanverfahren.Location = new System.Drawing.Point(762, 268);
            lbl_tanverfahren.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_tanverfahren.Name = "lbl_tanverfahren";
            lbl_tanverfahren.Size = new System.Drawing.Size(131, 25);
            lbl_tanverfahren.TabIndex = 71;
            lbl_tanverfahren.Text = "TAN-Verfahren:";
            // 
            // txt_verwendungszweck
            // 
            txt_verwendungszweck.Location = new System.Drawing.Point(985, 211);
            txt_verwendungszweck.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_verwendungszweck.Name = "txt_verwendungszweck";
            txt_verwendungszweck.Size = new System.Drawing.Size(492, 31);
            txt_verwendungszweck.TabIndex = 70;
            // 
            // lbl_verwendungszweck
            // 
            lbl_verwendungszweck.AutoSize = true;
            lbl_verwendungszweck.Location = new System.Drawing.Point(762, 218);
            lbl_verwendungszweck.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_verwendungszweck.Name = "lbl_verwendungszweck";
            lbl_verwendungszweck.Size = new System.Drawing.Size(170, 25);
            lbl_verwendungszweck.TabIndex = 69;
            lbl_verwendungszweck.Text = "Verwendungszweck:";
            // 
            // txt_betrag
            // 
            txt_betrag.Location = new System.Drawing.Point(985, 161);
            txt_betrag.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_betrag.Name = "txt_betrag";
            txt_betrag.Size = new System.Drawing.Size(492, 31);
            txt_betrag.TabIndex = 68;
            // 
            // lbl_betrag
            // 
            lbl_betrag.AutoSize = true;
            lbl_betrag.Location = new System.Drawing.Point(762, 168);
            lbl_betrag.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_betrag.Name = "lbl_betrag";
            lbl_betrag.Size = new System.Drawing.Size(67, 25);
            lbl_betrag.TabIndex = 67;
            lbl_betrag.Text = "Betrag:";
            // 
            // txt_empfängerbic
            // 
            txt_empfängerbic.Location = new System.Drawing.Point(985, 111);
            txt_empfängerbic.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_empfängerbic.Name = "txt_empfängerbic";
            txt_empfängerbic.Size = new System.Drawing.Size(492, 31);
            txt_empfängerbic.TabIndex = 66;
            // 
            // lbl_empfängerbic
            // 
            lbl_empfängerbic.AutoSize = true;
            lbl_empfängerbic.Location = new System.Drawing.Point(762, 118);
            lbl_empfängerbic.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_empfängerbic.Name = "lbl_empfängerbic";
            lbl_empfängerbic.Size = new System.Drawing.Size(136, 25);
            lbl_empfängerbic.TabIndex = 65;
            lbl_empfängerbic.Text = "Empfänger-BIC:";
            // 
            // txt_empfängeriban
            // 
            txt_empfängeriban.Location = new System.Drawing.Point(985, 61);
            txt_empfängeriban.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_empfängeriban.Name = "txt_empfängeriban";
            txt_empfängeriban.Size = new System.Drawing.Size(492, 31);
            txt_empfängeriban.TabIndex = 64;
            // 
            // lbl_empfängeriban
            // 
            lbl_empfängeriban.AutoSize = true;
            lbl_empfängeriban.Location = new System.Drawing.Point(762, 68);
            lbl_empfängeriban.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_empfängeriban.Name = "lbl_empfängeriban";
            lbl_empfängeriban.Size = new System.Drawing.Size(150, 25);
            lbl_empfängeriban.TabIndex = 63;
            lbl_empfängeriban.Text = "Empfänger-IBAN:";
            // 
            // txt_empfängername
            // 
            txt_empfängername.Location = new System.Drawing.Point(985, 11);
            txt_empfängername.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_empfängername.Name = "txt_empfängername";
            txt_empfängername.Size = new System.Drawing.Size(492, 31);
            txt_empfängername.TabIndex = 62;
            // 
            // lbl_empfängername
            // 
            lbl_empfängername.AutoSize = true;
            lbl_empfängername.Location = new System.Drawing.Point(762, 18);
            lbl_empfängername.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_empfängername.Name = "lbl_empfängername";
            lbl_empfängername.Size = new System.Drawing.Size(157, 25);
            lbl_empfängername.TabIndex = 61;
            lbl_empfängername.Text = "Empfänger-Name:";
            // 
            // txt_pin
            // 
            txt_pin.Location = new System.Drawing.Point(185, 361);
            txt_pin.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_pin.Name = "txt_pin";
            txt_pin.Size = new System.Drawing.Size(550, 31);
            txt_pin.TabIndex = 58;
            // 
            // lbl_pin
            // 
            lbl_pin.AutoSize = true;
            lbl_pin.Location = new System.Drawing.Point(20, 368);
            lbl_pin.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_pin.Name = "lbl_pin";
            lbl_pin.Size = new System.Drawing.Size(44, 25);
            lbl_pin.TabIndex = 57;
            lbl_pin.Text = "PIN:";
            // 
            // txt_userid
            // 
            txt_userid.Location = new System.Drawing.Point(185, 311);
            txt_userid.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_userid.Name = "txt_userid";
            txt_userid.Size = new System.Drawing.Size(550, 31);
            txt_userid.TabIndex = 56;
            // 
            // lbl_userid
            // 
            lbl_userid.AutoSize = true;
            lbl_userid.Location = new System.Drawing.Point(20, 318);
            lbl_userid.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_userid.Name = "lbl_userid";
            lbl_userid.Size = new System.Drawing.Size(76, 25);
            lbl_userid.TabIndex = 55;
            lbl_userid.Text = "User-ID:";
            // 
            // txt_hbci_version
            // 
            txt_hbci_version.Location = new System.Drawing.Point(185, 261);
            txt_hbci_version.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_hbci_version.Name = "txt_hbci_version";
            txt_hbci_version.Size = new System.Drawing.Size(550, 31);
            txt_hbci_version.TabIndex = 54;
            // 
            // lbl_hbci_version
            // 
            lbl_hbci_version.AutoSize = true;
            lbl_hbci_version.Location = new System.Drawing.Point(20, 268);
            lbl_hbci_version.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_hbci_version.Name = "lbl_hbci_version";
            lbl_hbci_version.Size = new System.Drawing.Size(120, 25);
            lbl_hbci_version.TabIndex = 53;
            lbl_hbci_version.Text = "HBCI-Version:";
            // 
            // txt_url
            // 
            txt_url.Location = new System.Drawing.Point(185, 211);
            txt_url.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_url.Name = "txt_url";
            txt_url.Size = new System.Drawing.Size(550, 31);
            txt_url.TabIndex = 52;
            // 
            // lbl_url
            // 
            lbl_url.AutoSize = true;
            lbl_url.Location = new System.Drawing.Point(20, 218);
            lbl_url.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_url.Name = "lbl_url";
            lbl_url.Size = new System.Drawing.Size(47, 25);
            lbl_url.TabIndex = 51;
            lbl_url.Text = "URL:";
            // 
            // txt_iban
            // 
            txt_iban.Location = new System.Drawing.Point(185, 161);
            txt_iban.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_iban.Name = "txt_iban";
            txt_iban.Size = new System.Drawing.Size(550, 31);
            txt_iban.TabIndex = 50;
            // 
            // lbl_iban
            // 
            lbl_iban.AutoSize = true;
            lbl_iban.Location = new System.Drawing.Point(20, 168);
            lbl_iban.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_iban.Name = "lbl_iban";
            lbl_iban.Size = new System.Drawing.Size(56, 25);
            lbl_iban.TabIndex = 49;
            lbl_iban.Text = "IBAN:";
            // 
            // txt_bic
            // 
            txt_bic.Location = new System.Drawing.Point(185, 111);
            txt_bic.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_bic.Name = "txt_bic";
            txt_bic.Size = new System.Drawing.Size(550, 31);
            txt_bic.TabIndex = 48;
            // 
            // lbl_bic
            // 
            lbl_bic.AutoSize = true;
            lbl_bic.Location = new System.Drawing.Point(20, 118);
            lbl_bic.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_bic.Name = "lbl_bic";
            lbl_bic.Size = new System.Drawing.Size(42, 25);
            lbl_bic.TabIndex = 47;
            lbl_bic.Text = "BIC:";
            // 
            // txt_bankleitzahl
            // 
            txt_bankleitzahl.Location = new System.Drawing.Point(185, 61);
            txt_bankleitzahl.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_bankleitzahl.Name = "txt_bankleitzahl";
            txt_bankleitzahl.Size = new System.Drawing.Size(216, 31);
            txt_bankleitzahl.TabIndex = 46;
            txt_bankleitzahl.TextChanged += Txt_bankleitzahl_TextChanged;
            // 
            // lbl_bankleitzahl
            // 
            lbl_bankleitzahl.AutoSize = true;
            lbl_bankleitzahl.Location = new System.Drawing.Point(20, 68);
            lbl_bankleitzahl.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_bankleitzahl.Name = "lbl_bankleitzahl";
            lbl_bankleitzahl.Size = new System.Drawing.Size(108, 25);
            lbl_bankleitzahl.TabIndex = 45;
            lbl_bankleitzahl.Text = "Bankleitzahl:";
            // 
            // txt_kontonummer
            // 
            txt_kontonummer.Location = new System.Drawing.Point(185, 11);
            txt_kontonummer.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_kontonummer.Name = "txt_kontonummer";
            txt_kontonummer.Size = new System.Drawing.Size(550, 31);
            txt_kontonummer.TabIndex = 44;
            txt_kontonummer.TextChanged += Txt_kontonummer_TextChanged;
            // 
            // lbl_kontonummer
            // 
            lbl_kontonummer.AutoSize = true;
            lbl_kontonummer.Location = new System.Drawing.Point(20, 18);
            lbl_kontonummer.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_kontonummer.Name = "lbl_kontonummer";
            lbl_kontonummer.Size = new System.Drawing.Size(131, 25);
            lbl_kontonummer.TabIndex = 43;
            lbl_kontonummer.Text = "Kontonummer:";
            // 
            // btn_zugelassene_tanverfahren
            // 
            btn_zugelassene_tanverfahren.Location = new System.Drawing.Point(1585, 525);
            btn_zugelassene_tanverfahren.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_zugelassene_tanverfahren.Name = "btn_zugelassene_tanverfahren";
            btn_zugelassene_tanverfahren.Size = new System.Drawing.Size(252, 75);
            btn_zugelassene_tanverfahren.TabIndex = 82;
            btn_zugelassene_tanverfahren.Text = "Zugelassene TAN-Verfahren";
            btn_zugelassene_tanverfahren.UseVisualStyleBackColor = true;
            btn_zugelassene_tanverfahren.Click += btn_zugelassene_tanverfahren_Click;
            // 
            // btn_bankdaten_laden
            // 
            btn_bankdaten_laden.Location = new System.Drawing.Point(1846, 525);
            btn_bankdaten_laden.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_bankdaten_laden.Name = "btn_bankdaten_laden";
            btn_bankdaten_laden.Size = new System.Drawing.Size(170, 75);
            btn_bankdaten_laden.TabIndex = 83;
            btn_bankdaten_laden.Text = "Bankdaten laden";
            btn_bankdaten_laden.UseVisualStyleBackColor = true;
            btn_bankdaten_laden.Click += btn_lade_bankdaten_Click;
            // 
            // btn_überweisungsdaten_laden
            // 
            btn_überweisungsdaten_laden.Location = new System.Drawing.Point(482, 611);
            btn_überweisungsdaten_laden.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_überweisungsdaten_laden.Name = "btn_überweisungsdaten_laden";
            btn_überweisungsdaten_laden.Size = new System.Drawing.Size(238, 75);
            btn_überweisungsdaten_laden.TabIndex = 84;
            btn_überweisungsdaten_laden.Text = "Überweisungsdaten laden";
            btn_überweisungsdaten_laden.UseVisualStyleBackColor = true;
            btn_überweisungsdaten_laden.Click += btn_lade_überweisungsdaten_Click;
            // 
            // txt_tan
            // 
            txt_tan.Location = new System.Drawing.Point(985, 361);
            txt_tan.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_tan.Name = "txt_tan";
            txt_tan.Size = new System.Drawing.Size(492, 31);
            txt_tan.TabIndex = 74;
            // 
            // lbl_tan
            // 
            lbl_tan.AutoSize = true;
            lbl_tan.Location = new System.Drawing.Point(762, 364);
            lbl_tan.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_tan.Name = "lbl_tan";
            lbl_tan.Size = new System.Drawing.Size(49, 25);
            lbl_tan.TabIndex = 73;
            lbl_tan.Text = "TAN:";
            // 
            // btn_konten_anzeigen
            // 
            btn_konten_anzeigen.Location = new System.Drawing.Point(295, 611);
            btn_konten_anzeigen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_konten_anzeigen.Name = "btn_konten_anzeigen";
            btn_konten_anzeigen.Size = new System.Drawing.Size(176, 75);
            btn_konten_anzeigen.TabIndex = 85;
            btn_konten_anzeigen.Text = "Konten anzeigen";
            btn_konten_anzeigen.UseVisualStyleBackColor = true;
            btn_konten_anzeigen.Click += btn_konten_anzeigen_Click;
            // 
            // btn_tan_medium_name_abfragen
            // 
            btn_tan_medium_name_abfragen.Location = new System.Drawing.Point(25, 611);
            btn_tan_medium_name_abfragen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_tan_medium_name_abfragen.Name = "btn_tan_medium_name_abfragen";
            btn_tan_medium_name_abfragen.Size = new System.Drawing.Size(260, 75);
            btn_tan_medium_name_abfragen.TabIndex = 86;
            btn_tan_medium_name_abfragen.Text = "TAN-Medium-Name abfragen";
            btn_tan_medium_name_abfragen.UseVisualStyleBackColor = true;
            btn_tan_medium_name_abfragen.Click += btn_tan_medium_name_abfragen_Click;
            // 
            // chk_tracing
            // 
            chk_tracing.AutoSize = true;
            chk_tracing.Checked = true;
            chk_tracing.CheckState = System.Windows.Forms.CheckState.Checked;
            chk_tracing.Location = new System.Drawing.Point(985, 418);
            chk_tracing.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            chk_tracing.Name = "chk_tracing";
            chk_tracing.Size = new System.Drawing.Size(275, 29);
            chk_tracing.TabIndex = 89;
            chk_tracing.Text = "HBCI-Nachrichten aufzeichnen";
            chk_tracing.UseVisualStyleBackColor = true;
            chk_tracing.CheckedChanged += chk_Tracing_CheckedChanged;
            // 
            // chk_tracingFormatted
            // 
            chk_tracingFormatted.AutoSize = true;
            chk_tracingFormatted.Checked = true;
            chk_tracingFormatted.CheckState = System.Windows.Forms.CheckState.Checked;
            chk_tracingFormatted.Location = new System.Drawing.Point(1360, 419);
            chk_tracingFormatted.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            chk_tracingFormatted.Name = "chk_tracingFormatted";
            chk_tracingFormatted.Size = new System.Drawing.Size(120, 29);
            chk_tracingFormatted.TabIndex = 90;
            chk_tracingFormatted.Text = "Formatiert";
            chk_tracingFormatted.UseVisualStyleBackColor = true;
            chk_tracingFormatted.CheckedChanged += chk_tracingFormatted_CheckedChanged;
            // 
            // lbl_bankleitzahl_zentrale
            // 
            lbl_bankleitzahl_zentrale.AutoSize = true;
            lbl_bankleitzahl_zentrale.Location = new System.Drawing.Point(424, 68);
            lbl_bankleitzahl_zentrale.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbl_bankleitzahl_zentrale.Name = "lbl_bankleitzahl_zentrale";
            lbl_bankleitzahl_zentrale.Size = new System.Drawing.Size(79, 25);
            lbl_bankleitzahl_zentrale.TabIndex = 91;
            lbl_bankleitzahl_zentrale.Text = "Zentrale:";
            // 
            // txt_bankleitzahl_zentrale
            // 
            txt_bankleitzahl_zentrale.Location = new System.Drawing.Point(520, 61);
            txt_bankleitzahl_zentrale.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_bankleitzahl_zentrale.Name = "txt_bankleitzahl_zentrale";
            txt_bankleitzahl_zentrale.Size = new System.Drawing.Size(216, 31);
            txt_bankleitzahl_zentrale.TabIndex = 47;
            // 
            // txt_tan_medium
            // 
            txt_tan_medium.Location = new System.Drawing.Point(985, 311);
            txt_tan_medium.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            txt_tan_medium.Name = "txt_tan_medium";
            txt_tan_medium.Size = new System.Drawing.Size(492, 31);
            txt_tan_medium.TabIndex = 73;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(762, 319);
            label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(122, 25);
            label1.TabIndex = 92;
            label1.Text = "TAN-Medium:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(20, 419);
            label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(142, 25);
            label2.TabIndex = 94;
            label2.Text = "Umsatzabruf ab:";
            // 
            // date_umsatzabruf_von
            // 
            date_umsatzabruf_von.Location = new System.Drawing.Point(220, 414);
            date_umsatzabruf_von.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            date_umsatzabruf_von.Name = "date_umsatzabruf_von";
            date_umsatzabruf_von.Size = new System.Drawing.Size(516, 31);
            date_umsatzabruf_von.TabIndex = 95;
            // 
            // chk_umsatzabruf_von
            // 
            chk_umsatzabruf_von.AutoSize = true;
            chk_umsatzabruf_von.Location = new System.Drawing.Point(185, 419);
            chk_umsatzabruf_von.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            chk_umsatzabruf_von.Name = "chk_umsatzabruf_von";
            chk_umsatzabruf_von.Size = new System.Drawing.Size(22, 21);
            chk_umsatzabruf_von.TabIndex = 96;
            chk_umsatzabruf_von.UseVisualStyleBackColor = true;
            // 
            // chk_umsatzabruf_bis
            // 
            chk_umsatzabruf_bis.AutoSize = true;
            chk_umsatzabruf_bis.Location = new System.Drawing.Point(185, 471);
            chk_umsatzabruf_bis.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            chk_umsatzabruf_bis.Name = "chk_umsatzabruf_bis";
            chk_umsatzabruf_bis.Size = new System.Drawing.Size(22, 21);
            chk_umsatzabruf_bis.TabIndex = 99;
            chk_umsatzabruf_bis.UseVisualStyleBackColor = true;
            // 
            // date_umsatzabruf_bis
            // 
            date_umsatzabruf_bis.Location = new System.Drawing.Point(220, 466);
            date_umsatzabruf_bis.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            date_umsatzabruf_bis.Name = "date_umsatzabruf_bis";
            date_umsatzabruf_bis.Size = new System.Drawing.Size(516, 31);
            date_umsatzabruf_bis.TabIndex = 98;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(20, 471);
            label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(145, 25);
            label3.TabIndex = 97;
            label3.Text = "Umsatzabruf bis:";
            // 
            // btn_daueraufträge_abholen
            // 
            btn_daueraufträge_abholen.Location = new System.Drawing.Point(1014, 525);
            btn_daueraufträge_abholen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_daueraufträge_abholen.Name = "btn_daueraufträge_abholen";
            btn_daueraufträge_abholen.Size = new System.Drawing.Size(210, 75);
            btn_daueraufträge_abholen.TabIndex = 100;
            btn_daueraufträge_abholen.Text = "Daueraufträge abholen";
            btn_daueraufträge_abholen.UseVisualStyleBackColor = true;
            btn_daueraufträge_abholen.Click += btn_daueraufträge_abholen_Click;
            // 
            // btn_terminueberweisungen_abholen
            // 
            btn_terminueberweisungen_abholen.Location = new System.Drawing.Point(731, 610);
            btn_terminueberweisungen_abholen.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btn_terminueberweisungen_abholen.Name = "btn_terminueberweisungen_abholen";
            btn_terminueberweisungen_abholen.Size = new System.Drawing.Size(210, 75);
            btn_terminueberweisungen_abholen.TabIndex = 101;
            btn_terminueberweisungen_abholen.Text = "Terminüberweisungen abholen";
            btn_terminueberweisungen_abholen.UseVisualStyleBackColor = true;
            btn_terminueberweisungen_abholen.Click += btn_terminueberweisungen_abholen_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(2034, 1459);
            Controls.Add(btn_terminueberweisungen_abholen);
            Controls.Add(btn_daueraufträge_abholen);
            Controls.Add(chk_umsatzabruf_bis);
            Controls.Add(date_umsatzabruf_bis);
            Controls.Add(label3);
            Controls.Add(chk_umsatzabruf_von);
            Controls.Add(date_umsatzabruf_von);
            Controls.Add(label2);
            Controls.Add(txt_tan_medium);
            Controls.Add(label1);
            Controls.Add(txt_bankleitzahl_zentrale);
            Controls.Add(lbl_bankleitzahl_zentrale);
            Controls.Add(chk_tracingFormatted);
            Controls.Add(chk_tracing);
            Controls.Add(btn_tan_medium_name_abfragen);
            Controls.Add(btn_konten_anzeigen);
            Controls.Add(txt_tan);
            Controls.Add(lbl_tan);
            Controls.Add(btn_überweisungsdaten_laden);
            Controls.Add(btn_bankdaten_laden);
            Controls.Add(btn_zugelassene_tanverfahren);
            Controls.Add(txt_hbci_meldung);
            Controls.Add(btn_auftrag_bestätigen_tan);
            Controls.Add(btn_überweisen);
            Controls.Add(camt_053_abholen);
            Controls.Add(camt_052_abholen);
            Controls.Add(btn_umsätze_abholen);
            Controls.Add(btn_kontostand_abfragen);
            Controls.Add(btn_synchronisation);
            Controls.Add(pBox_tan);
            Controls.Add(txt_tanverfahren);
            Controls.Add(lbl_tanverfahren);
            Controls.Add(txt_verwendungszweck);
            Controls.Add(lbl_verwendungszweck);
            Controls.Add(txt_betrag);
            Controls.Add(lbl_betrag);
            Controls.Add(txt_empfängerbic);
            Controls.Add(lbl_empfängerbic);
            Controls.Add(txt_empfängeriban);
            Controls.Add(lbl_empfängeriban);
            Controls.Add(txt_empfängername);
            Controls.Add(lbl_empfängername);
            Controls.Add(txt_pin);
            Controls.Add(lbl_pin);
            Controls.Add(txt_userid);
            Controls.Add(lbl_userid);
            Controls.Add(txt_hbci_version);
            Controls.Add(lbl_hbci_version);
            Controls.Add(txt_url);
            Controls.Add(lbl_url);
            Controls.Add(txt_iban);
            Controls.Add(lbl_iban);
            Controls.Add(txt_bic);
            Controls.Add(lbl_bic);
            Controls.Add(txt_bankleitzahl);
            Controls.Add(lbl_bankleitzahl);
            Controls.Add(txt_kontonummer);
            Controls.Add(lbl_kontonummer);
            Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            Name = "MainForm";
            Text = "libfintx Test Framework";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize) pBox_tan).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        internal System.Windows.Forms.TextBox txt_hbci_meldung;
        internal System.Windows.Forms.Button btn_auftrag_bestätigen_tan;
        internal System.Windows.Forms.Button btn_überweisen;
        internal System.Windows.Forms.Button camt_053_abholen;
        internal System.Windows.Forms.Button camt_052_abholen;
        internal System.Windows.Forms.Button btn_umsätze_abholen;
        internal System.Windows.Forms.Button btn_kontostand_abfragen;
        internal System.Windows.Forms.Button btn_synchronisation;
        internal System.Windows.Forms.PictureBox pBox_tan;
        internal System.Windows.Forms.TextBox txt_tanverfahren;
        internal System.Windows.Forms.Label lbl_tanverfahren;
        internal System.Windows.Forms.TextBox txt_verwendungszweck;
        internal System.Windows.Forms.Label lbl_verwendungszweck;
        internal System.Windows.Forms.TextBox txt_betrag;
        internal System.Windows.Forms.Label lbl_betrag;
        internal System.Windows.Forms.TextBox txt_empfängerbic;
        internal System.Windows.Forms.Label lbl_empfängerbic;
        internal System.Windows.Forms.TextBox txt_empfängeriban;
        internal System.Windows.Forms.Label lbl_empfängeriban;
        internal System.Windows.Forms.TextBox txt_empfängername;
        internal System.Windows.Forms.Label lbl_empfängername;
        internal System.Windows.Forms.TextBox txt_pin;
        internal System.Windows.Forms.Label lbl_pin;
        internal System.Windows.Forms.TextBox txt_userid;
        internal System.Windows.Forms.Label lbl_userid;
        internal System.Windows.Forms.TextBox txt_hbci_version;
        internal System.Windows.Forms.Label lbl_hbci_version;
        internal System.Windows.Forms.TextBox txt_url;
        internal System.Windows.Forms.Label lbl_url;
        internal System.Windows.Forms.TextBox txt_iban;
        internal System.Windows.Forms.Label lbl_iban;
        internal System.Windows.Forms.TextBox txt_bic;
        internal System.Windows.Forms.Label lbl_bic;
        internal System.Windows.Forms.TextBox txt_bankleitzahl;
        internal System.Windows.Forms.Label lbl_bankleitzahl;
        internal System.Windows.Forms.TextBox txt_kontonummer;
        internal System.Windows.Forms.Label lbl_kontonummer;
        internal System.Windows.Forms.Button btn_zugelassene_tanverfahren;
        internal System.Windows.Forms.Button btn_bankdaten_laden;
        internal System.Windows.Forms.Button btn_überweisungsdaten_laden;
        internal System.Windows.Forms.TextBox txt_tan;
        internal System.Windows.Forms.Label lbl_tan;
        internal System.Windows.Forms.Button btn_konten_anzeigen;
        internal System.Windows.Forms.Button btn_tan_medium_name_abfragen;
        private System.Windows.Forms.CheckBox chk_tracing;
        private System.Windows.Forms.CheckBox chk_tracingFormatted;
        internal System.Windows.Forms.Label lbl_bankleitzahl_zentrale;
        internal System.Windows.Forms.TextBox txt_bankleitzahl_zentrale;
        internal System.Windows.Forms.TextBox txt_tan_medium;
        internal System.Windows.Forms.Label label1;
        internal System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker date_umsatzabruf_von;
        private System.Windows.Forms.CheckBox chk_umsatzabruf_von;
        private System.Windows.Forms.CheckBox chk_umsatzabruf_bis;
        private System.Windows.Forms.DateTimePicker date_umsatzabruf_bis;
        internal System.Windows.Forms.Label label3;
        internal System.Windows.Forms.Button btn_daueraufträge_abholen;
        internal System.Windows.Forms.Button btn_terminueberweisungen_abholen;
    }
}

