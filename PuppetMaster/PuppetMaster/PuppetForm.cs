using Client;
using Grpc.Net.Client;
using Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class PuppetForm : Form
    {
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.TextBox tb_serverID;
        private System.Windows.Forms.TextBox tb_insertURL;
        private System.Windows.Forms.TextBox tb_insertMin;
        private System.Windows.Forms.TextBox tb_insertMax;

        private System.Windows.Forms.Button bt_insertServer;
        private System.Windows.Forms.TextBox tb_showServ;
        private Label label5;
        private TextBox tb_PartName;
        private Label label6;
        private TextBox tb_R;
        private Label label7;
        private TextBox tb_PartServs;
        private Label label8;
        private TextBox tb_ClientUser;
        private Label label9;
        private TextBox tb_ClientURL;
        private Label label10;
        private TextBox tb_ClientScript;
        private Button bt_AddPartition;
        private Button bt_AddClient;
        private Button bt_deleteClient;
        private static PuppetList puppetList;
        private Button bt_FrServer;
        private Button bt_UnFrServer;
        private Button bt_CrSserver;
        private Label label11;
        private Button bt_SleepServ;
        private TextBox tb_WaitValue;
        private Label label12;
        private Button button1;
        private Button button2;

        // Mudar para o nosso folder
        //static readonly string rootFolder = @"C:\Temp\Data\";
        static readonly string textFile = Directory.GetCurrentDirectory() + @"pm_script1";

        public PuppetForm()
        {
            puppetList = new PuppetList();
            InitializeComponent();
        }

        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tb_serverID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tb_insertURL = new System.Windows.Forms.TextBox();
            this.tb_insertMin = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tb_insertMax = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.bt_insertServer = new System.Windows.Forms.Button();
            this.tb_showServ = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tb_PartName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tb_R = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tb_PartServs = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tb_ClientUser = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.tb_ClientURL = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tb_ClientScript = new System.Windows.Forms.TextBox();
            this.bt_AddPartition = new System.Windows.Forms.Button();
            this.bt_AddClient = new System.Windows.Forms.Button();
            this.bt_deleteClient = new System.Windows.Forms.Button();
            this.bt_FrServer = new System.Windows.Forms.Button();
            this.bt_UnFrServer = new System.Windows.Forms.Button();
            this.bt_CrSserver = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.bt_SleepServ = new System.Windows.Forms.Button();
            this.tb_WaitValue = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tb_serverID
            // 
            this.tb_serverID.Location = new System.Drawing.Point(93, 23);
            this.tb_serverID.Name = "tb_serverID";
            this.tb_serverID.Size = new System.Drawing.Size(129, 23);
            this.tb_serverID.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(28, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Server ID :";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(28, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "URL :";
            // 
            // tb_insertURL
            // 
            this.tb_insertURL.Location = new System.Drawing.Point(68, 80);
            this.tb_insertURL.Name = "tb_insertURL";
            this.tb_insertURL.Size = new System.Drawing.Size(245, 23);
            this.tb_insertURL.TabIndex = 3;
            // 
            // tb_insertMin
            // 
            this.tb_insertMin.Location = new System.Drawing.Point(133, 109);
            this.tb_insertMin.Name = "tb_insertMin";
            this.tb_insertMin.Size = new System.Drawing.Size(180, 23);
            this.tb_insertMin.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(28, 112);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 15);
            this.label3.TabIndex = 5;
            this.label3.Text = "Minimum delay :";
            // 
            // tb_insertMax
            // 
            this.tb_insertMax.Location = new System.Drawing.Point(133, 140);
            this.tb_insertMax.Name = "tb_insertMax";
            this.tb_insertMax.Size = new System.Drawing.Size(180, 23);
            this.tb_insertMax.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(28, 146);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(99, 15);
            this.label4.TabIndex = 7;
            this.label4.Text = "Maximum delay :";
            // 
            // bt_insertServer
            // 
            this.bt_insertServer.Location = new System.Drawing.Point(28, 170);
            this.bt_insertServer.Name = "bt_insertServer";
            this.bt_insertServer.Size = new System.Drawing.Size(285, 24);
            this.bt_insertServer.TabIndex = 8;
            this.bt_insertServer.Text = "Add Server";
            this.bt_insertServer.UseVisualStyleBackColor = true;
            this.bt_insertServer.Click += new System.EventHandler(this.bt_AddServer);
            // 
            // tb_showServ
            // 
            this.tb_showServ.Location = new System.Drawing.Point(28, 200);
            this.tb_showServ.Multiline = true;
            this.tb_showServ.Name = "tb_showServ";
            this.tb_showServ.Size = new System.Drawing.Size(796, 182);
            this.tb_showServ.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(332, 23);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 15);
            this.label5.TabIndex = 11;
            this.label5.Text = "Partition Name :";
            this.label5.Click += new System.EventHandler(this.label5_Click);
            // 
            // tb_PartName
            // 
            this.tb_PartName.Location = new System.Drawing.Point(431, 20);
            this.tb_PartName.Name = "tb_PartName";
            this.tb_PartName.Size = new System.Drawing.Size(127, 23);
            this.tb_PartName.TabIndex = 12;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(330, 50);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(126, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "Number of Servers (r) :";
            // 
            // tb_R
            // 
            this.tb_R.Location = new System.Drawing.Point(462, 47);
            this.tb_R.Name = "tb_R";
            this.tb_R.Size = new System.Drawing.Size(96, 23);
            this.tb_R.TabIndex = 14;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(332, 73);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(183, 15);
            this.label7.TabIndex = 15;
            this.label7.Text = "Server ID\'s (separated by spaces) :";
            this.label7.Click += new System.EventHandler(this.label7_Click);
            // 
            // tb_PartServs
            // 
            this.tb_PartServs.Location = new System.Drawing.Point(332, 91);
            this.tb_PartServs.Name = "tb_PartServs";
            this.tb_PartServs.Size = new System.Drawing.Size(226, 23);
            this.tb_PartServs.TabIndex = 16;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(586, 23);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(100, 15);
            this.label8.TabIndex = 17;
            this.label8.Text = "Client Username :";
            // 
            // tb_ClientUser
            // 
            this.tb_ClientUser.Location = new System.Drawing.Point(692, 19);
            this.tb_ClientUser.Name = "tb_ClientUser";
            this.tb_ClientUser.Size = new System.Drawing.Size(132, 23);
            this.tb_ClientUser.TabIndex = 18;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(586, 83);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(68, 15);
            this.label9.TabIndex = 19;
            this.label9.Text = "Client URL :";
            // 
            // tb_ClientURL
            // 
            this.tb_ClientURL.Location = new System.Drawing.Point(660, 80);
            this.tb_ClientURL.Name = "tb_ClientURL";
            this.tb_ClientURL.Size = new System.Drawing.Size(164, 23);
            this.tb_ClientURL.TabIndex = 20;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(586, 112);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(62, 15);
            this.label10.TabIndex = 21;
            this.label10.Text = "Script file :";
            // 
            // tb_ClientScript
            // 
            this.tb_ClientScript.Location = new System.Drawing.Point(660, 109);
            this.tb_ClientScript.Name = "tb_ClientScript";
            this.tb_ClientScript.Size = new System.Drawing.Size(164, 23);
            this.tb_ClientScript.TabIndex = 22;
            // 
            // bt_AddPartition
            // 
            this.bt_AddPartition.Location = new System.Drawing.Point(332, 120);
            this.bt_AddPartition.Name = "bt_AddPartition";
            this.bt_AddPartition.Size = new System.Drawing.Size(226, 23);
            this.bt_AddPartition.TabIndex = 23;
            this.bt_AddPartition.Text = "Add Partition";
            this.bt_AddPartition.UseVisualStyleBackColor = true;
            this.bt_AddPartition.Click += new System.EventHandler(this.bt_AddPart);
            // 
            // bt_AddClient
            // 
            this.bt_AddClient.Location = new System.Drawing.Point(586, 138);
            this.bt_AddClient.Name = "bt_AddClient";
            this.bt_AddClient.Size = new System.Drawing.Size(238, 23);
            this.bt_AddClient.TabIndex = 24;
            this.bt_AddClient.Text = "Add Client";
            this.bt_AddClient.UseVisualStyleBackColor = true;
            this.bt_AddClient.Click += new System.EventHandler(this.bt_AddCli);
            // 
            // bt_deleteClient
            // 
            this.bt_deleteClient.Location = new System.Drawing.Point(586, 48);
            this.bt_deleteClient.Name = "bt_deleteClient";
            this.bt_deleteClient.Size = new System.Drawing.Size(238, 23);
            this.bt_deleteClient.TabIndex = 25;
            this.bt_deleteClient.Text = "Delete Client";
            this.bt_deleteClient.UseVisualStyleBackColor = true;
            this.bt_deleteClient.Click += new System.EventHandler(this.bt_DelCli);
            // 
            // bt_FrServer
            // 
            this.bt_FrServer.Location = new System.Drawing.Point(33, 50);
            this.bt_FrServer.Name = "bt_FrServer";
            this.bt_FrServer.Size = new System.Drawing.Size(138, 23);
            this.bt_FrServer.TabIndex = 26;
            this.bt_FrServer.Text = "Freeze";
            this.bt_FrServer.UseVisualStyleBackColor = true;
            this.bt_FrServer.Click += new System.EventHandler(this.bt_FreezeServer);
            // 
            // bt_UnFrServer
            // 
            this.bt_UnFrServer.Location = new System.Drawing.Point(177, 50);
            this.bt_UnFrServer.Name = "bt_UnFrServer";
            this.bt_UnFrServer.Size = new System.Drawing.Size(136, 23);
            this.bt_UnFrServer.TabIndex = 27;
            this.bt_UnFrServer.Text = "Unfreeze";
            this.bt_UnFrServer.UseVisualStyleBackColor = true;
            this.bt_UnFrServer.Click += new System.EventHandler(this.bt_UnfreezeServer);
            // 
            // bt_CrSserver
            // 
            this.bt_CrSserver.Location = new System.Drawing.Point(228, 23);
            this.bt_CrSserver.Name = "bt_CrSserver";
            this.bt_CrSserver.Size = new System.Drawing.Size(85, 23);
            this.bt_CrSserver.TabIndex = 28;
            this.bt_CrSserver.Text = "Crash";
            this.bt_CrSserver.UseVisualStyleBackColor = true;
            this.bt_CrSserver.Click += new System.EventHandler(this.bt_CrashServer);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(330, 148);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(95, 15);
            this.label11.TabIndex = 29;
            this.label11.Text = "PM Wait (in ms):";
            // 
            // bt_SleepServ
            // 
            this.bt_SleepServ.Location = new System.Drawing.Point(506, 146);
            this.bt_SleepServ.Name = "bt_SleepServ";
            this.bt_SleepServ.Size = new System.Drawing.Size(52, 23);
            this.bt_SleepServ.TabIndex = 30;
            this.bt_SleepServ.Text = "Sleep";
            this.bt_SleepServ.UseVisualStyleBackColor = true;
            this.bt_SleepServ.Click += new System.EventHandler(this.bt_WaitPM);
            // 
            // tb_WaitValue
            // 
            this.tb_WaitValue.Location = new System.Drawing.Point(424, 146);
            this.tb_WaitValue.Name = "tb_WaitValue";
            this.tb_WaitValue.Size = new System.Drawing.Size(76, 23);
            this.tb_WaitValue.TabIndex = 31;
            this.tb_WaitValue.TextChanged += new System.EventHandler(this.tb_WaitValue_TextChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(586, 175);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(94, 15);
            this.label12.TabIndex = 32;
            this.label12.Text = "PM Script name:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(686, 171);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(138, 23);
            this.button1.TabIndex = 34;
            this.button1.Text = "Browse Files";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.bt_PMScriptRun);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(332, 171);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(226, 23);
            this.button2.TabIndex = 35;
            this.button2.Text = "Status";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.bt_Status);
            // 
            // PuppetForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(869, 394);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.tb_WaitValue);
            this.Controls.Add(this.bt_SleepServ);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.bt_CrSserver);
            this.Controls.Add(this.bt_UnFrServer);
            this.Controls.Add(this.bt_FrServer);
            this.Controls.Add(this.bt_deleteClient);
            this.Controls.Add(this.bt_AddClient);
            this.Controls.Add(this.bt_AddPartition);
            this.Controls.Add(this.tb_ClientScript);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.tb_ClientURL);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.tb_ClientUser);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tb_PartServs);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tb_R);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.tb_PartName);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tb_showServ);
            this.Controls.Add(this.bt_insertServer);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tb_insertMax);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tb_insertMin);
            this.Controls.Add(this.tb_insertURL);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tb_serverID);
            this.Name = "PuppetForm";
            this.Text = "PuppetMaster";
            this.Load += new System.EventHandler(this.PuppetForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        static void Main()
        {

            AllocConsole();
            Application.Run(new PuppetForm());

        }

        private void bt_AddServer(object sender, EventArgs e)
        {
            if (tb_serverID.Text != "" && tb_insertURL.Text != "" && tb_insertMin.Text != "" && tb_insertMax.Text != "")
            {
                puppetList.AddGUIServer(tb_serverID.Text, tb_insertURL.Text, tb_insertMin.Text, tb_insertMax.Text);
                tb_showServ.Text = puppetList.ShowAll();
            }
        }

        private void bt_DelServer(object sender, EventArgs e)
        {
            if (tb_serverID.Text != "")
            {
                puppetList.DelServer(tb_serverID.Text);
                tb_showServ.Text = puppetList.ShowAll();
            }
        }

        private void bt_AddPart(object sender, EventArgs e)
        {
            if (tb_PartName.Text != "" && tb_R.Text != "" && tb_PartServs.Text != "")
            {
                puppetList.AddPartition(tb_R.Text, tb_PartName.Text,tb_PartServs.Text);
                tb_showServ.Text = puppetList.ShowAll();
            }
        }

        private void bt_AddCli(object sender, EventArgs e)
        {
            if (tb_ClientUser.Text != "" && tb_ClientURL.Text != "" && tb_ClientScript.Text != "")
            {
                puppetList.AddClient(tb_ClientUser.Text, tb_ClientURL.Text, tb_ClientScript.Text);
                tb_showServ.Text = puppetList.ShowAll();
            }
        }

        private void bt_DelCli(object sender, EventArgs e)
        {
            if (tb_ClientUser.Text != "")
            {
                puppetList.DelClient(tb_ClientUser.Text);
                tb_showServ.Text = puppetList.ShowAll();
            }
        }

        private void PuppetForm_Load(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void bt_FreezeServer(object sender, EventArgs e)
        {
            if (tb_serverID.Text != "")
            {
                List<Server> puppet_servers = puppetList.getServList();
                foreach (Server s in puppet_servers)
                {
                    Console.WriteLine($"Freeze ID do s: {s.getID()}");
                    Console.WriteLine($"Freeze ID do argumento: {tb_serverID.Text}");

                    if (s.getID() == tb_serverID.Text)
                    {
                        Console.WriteLine($"Freezing Server {tb_serverID.Text}...");
                        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                        GrpcChannel channel_server = GrpcChannel.ForAddress(s.getURL());
                        GStoreServices.GStoreServicesClient server_client = new GStoreServices.GStoreServicesClient(channel_server);
                        server_client.FreezeServerAsync(new FreezeMessage { });
                    }
                }
            }
            tb_showServ.Text = puppetList.ShowAll();
        }

        private void bt_UnfreezeServer(object sender, EventArgs e)
        {
            if (tb_serverID.Text != "")
            {
                List<Server> puppet_servers = puppetList.getServList();
                foreach (Server s in puppet_servers)
                {
                    Console.WriteLine($"Unfreeze ID do s: {s.getID()}");
                    Console.WriteLine($"Unfreeze ID do argumento: {tb_serverID.Text}");

                    if (s.getID() == tb_serverID.Text)
                    {
                        Console.WriteLine($"Unfreezing Server {tb_serverID.Text}...");
                        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                        GrpcChannel channel_server = GrpcChannel.ForAddress(s.getURL());
                        GStoreServices.GStoreServicesClient server_client = new GStoreServices.GStoreServicesClient(channel_server);
                        server_client.UnfreezeServerAsync(new UnfreezeMessage { });
                    }
                }
            }
            tb_showServ.Text = puppetList.ShowAll();
        }

        private void bt_CrashServer(object sender, EventArgs e)
        {
            if (tb_serverID.Text != "")
            {
                bool b = false;
                foreach (Server serv in puppetList.getServList().ToList())
                {
                    if (serv.getID().Equals(tb_serverID.Text))
                    {
                        b = true;
                    }
                }

                if (b)
                {
                    puppetList.DelServer(tb_serverID.Text);
                }

                //propagar para todos
                puppetList.propagateCrash(tb_serverID.Text);
            }
            tb_showServ.Text = puppetList.ShowAll();
        }

        private void bt_WaitPM(object sender, EventArgs e)
        {
            Console.WriteLine("***************************************************************************");
            Console.WriteLine($"PuppetMaster vai ficar em sleep durante {tb_WaitValue.Text} milisegundos.");
            Thread.Sleep(Convert.ToInt32(tb_WaitValue.Text));
            Console.WriteLine("PuppetMaster vai continuar....");
            Console.WriteLine("***************************************************************************");
            tb_showServ.Text = puppetList.ShowAll();
        }

        private void tb_WaitValue_TextChanged(object sender, EventArgs e)
        {

        }

        private void bt_PMScriptRun(object sender, EventArgs e)
        {

            string PMScript = "";
            Thread t = new Thread((ThreadStart)(() =>
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                string path = Directory.GetCurrentDirectory();
                openFileDialog1.InitialDirectory = Path.GetFullPath(Path.Combine(path, @"..\..\..\"));
                openFileDialog1.RestoreDirectory = true;
                openFileDialog1.Title = "Browse Text Files";
                openFileDialog1.DefaultExt = "txt";
                openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.CheckFileExists = true;
                openFileDialog1.CheckPathExists = true;
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    PMScript = openFileDialog1.FileName;

                }

            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();



            Console.WriteLine(PMScript);

            if (PMScript != "")
            {
                //puppetList.startPM();
                // Verificacao da existencia do ficheiro
                Console.WriteLine("Verificar se o ficheiro existe....");

                if (File.Exists(PMScript))
                {
                    Console.WriteLine("O ficheiro existe....");
                    // PRIMEIRA PASSAGEM
                    //- Adiciona a lista geral os servers, partitions e clients, ignorando o resto.
                    using (StreamReader file = new StreamReader(PMScript))
                    {
                        string ln;

                        while ((ln = file.ReadLine()) != null)
                        {
                            // Parsing das linhas do script...
                            string[] parts = ln.Split(new[] { ' ' });

                            if (parts[0].Equals("ReplicationFactor") && parts.Count() == 2)
                            {
                                continue; // Ignora
                            }
                            else if (parts[0].Equals("Partition"))
                            {
                                Console.WriteLine("Found a partition to create...");
                                //join dos ids servidores
                                string servers_id = "";
                                for (int i = 3; i != parts.Length; i++)
                                {
                                    servers_id += parts[i] + " ";
                                }
                                //Console.WriteLine($"Lista final dos servidores da partição: {servers_id}");
                                puppetList.AddPartition(parts[1], parts[2], servers_id);
                                //Console.WriteLine($"Comprimento da lista das partições (após inserção) : {puppetList.getPartList().Count}");
                            }
                            else if (parts[0].Equals("Server") && parts.Count() == 5)
                            {
                                Console.WriteLine("Found a server to create...");
                                puppetList.AddServerInfo(parts[1], parts[2], parts[3], parts[4]);
                                //Console.WriteLine($"Comprimento da lista dos servidores (após inserção) : {puppetList.getServList().Count}");
                            }
                        }
                        file.Close();
                    }

                    // Depois da primeira passagem, lança todos os servidores / clientes
                    //partições já estão completas
                    puppetList.LaunchServers();
                    //puppetList.LaunchClients();

                    //enviar partições para os clientes e servidores
                    puppetList.sendPartitionToProcesses();


                    // SEGUNDA PASSAGEM
                    // - Nesta passagem so nos interessa os waits, freezes, unfreezes e status

                    using (StreamReader file = new StreamReader(PMScript))
                    {
                        string ln;

                        while ((ln = file.ReadLine()) != null)
                        {
                            // Parsing das linhas do script...
                            string[] parts = ln.Split(new[] { ' ' });

                            if (parts[0].Equals("Status") && parts.Count() == 1)
                            {
                                Console.WriteLine("PuppetMaster: Status Message");
                                //iterate over every server and client in the mapping
                                List<Client> puppet_clients = puppetList.getCliList();
                                List<Server> puppet_servers = puppetList.getServList();
                                foreach (Client c in puppet_clients)
                                {
                                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                    GrpcChannel channel = GrpcChannel.ForAddress(c.getURL());
                                    ClientServices.ClientServicesClient client = new ClientServices.ClientServicesClient(channel);
                                    client.StatusClientAsync(new AskStatusClient { });
                                }
                                foreach (Server s in puppet_servers)
                                {
                                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                    GrpcChannel channel_server = GrpcChannel.ForAddress(s.getURL());
                                    GStoreServices.GStoreServicesClient server_client = new GStoreServices.GStoreServicesClient(channel_server);
                                    server_client.StatusServerAsync(new AskStatusServer { });
                                }
                            }
                            else if (parts[0].Equals("Wait") && parts.Count() == 2)
                            {
                                Console.WriteLine("***************************************************************************");
                                Console.WriteLine($"PuppetMaster vai ficar em sleep durante {parts[1]} milisegundos.");
                                Thread.Sleep(Convert.ToInt32(parts[1]));
                                Console.WriteLine("PuppetMaster vai continuar....");
                                Console.WriteLine("***************************************************************************");
                            }
                            else if (parts[0].Equals("Freeze") && parts.Count() == 2)
                            {
                                Console.WriteLine("PuppetMaster: Freeze Message");
                                //sending freeze request to pretended server
                                List<Server> puppet_servers = puppetList.getServList();
                                foreach (Server s in puppet_servers)
                                {
                                    Console.WriteLine($"Freeze ID do s: {s.getID()}");
                                    Console.WriteLine($"Freeze ID do argumento: {parts[1]}");

                                    if (s.getID() == parts[1])
                                    {
                                        Console.WriteLine($"Freezing Server {parts[1]}...");
                                        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                        GrpcChannel channel_server = GrpcChannel.ForAddress(s.getURL());
                                        GStoreServices.GStoreServicesClient server_client = new GStoreServices.GStoreServicesClient(channel_server);
                                        server_client.FreezeServerAsync(new FreezeMessage { });
                                    }
                                }
                            }
                            else if (parts[0].Equals("Unfreeze") && parts.Count() == 2)
                            {
                                Console.WriteLine("PuppetMaster: Unfreeze Message");
                                //sending unfreeze request to pretended server
                                List<Server> puppet_servers = puppetList.getServList();
                                foreach (Server s in puppet_servers)
                                {
                                    Console.WriteLine($"Unfreeze ID do s: {s.getID()}");
                                    Console.WriteLine($"Unfreeze ID do argumento: {parts[1]}");

                                    if (s.getID() == parts[1])
                                    {
                                        Console.WriteLine($"Unfreezing Server {parts[1]}...");
                                        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                                        GrpcChannel channel_server = GrpcChannel.ForAddress(s.getURL());
                                        GStoreServices.GStoreServicesClient server_client = new GStoreServices.GStoreServicesClient(channel_server);
                                        server_client.UnfreezeServerAsync(new UnfreezeMessage { });
                                    }
                                }
                            }
                            else if (parts[0].Equals("Crash") && parts.Count() == 2)
                            {
                                Console.WriteLine($"Puppet Master found a crash. Crashing server {parts[1]}");
                                bool b = false;
                                foreach (Server serv in puppetList.getServList().ToList())
                                {
                                    if (serv.getID().Equals(parts[1]))
                                    {
                                        b = true;
                                    }
                                }

                                if (b)
                                {
                                    puppetList.DelServer(parts[1]);
                                }

                                //propagar para todos
                                puppetList.propagateCrash(parts[1]);
                            }
                            else if (parts[0].Equals("Client") && parts.Count() == 4)
                            {
                                Console.WriteLine("Found a cliente to create...");
                                puppetList.AddClient(parts[1], parts[2], parts[3]);
                                //Console.WriteLine($"Comprimento da lista dos clientes (após inserção) : {puppetList.getCliList().Count}");
                            }
                        }
                        file.Close();
                    }
                }
                tb_showServ.Text = puppetList.ShowAll();
            }
        }

        private void bt_Status(object sender, EventArgs e)
        {
            Console.WriteLine("PuppetMaster: Status Message");
            //iterate over every server and client in the mapping
            foreach (Client c in puppetList.getCliList())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(c.getURL());
                ClientServices.ClientServicesClient client = new ClientServices.ClientServicesClient(channel);
                client.StatusClientAsync(new AskStatusClient { });
            }
            foreach (Server s in puppetList.getServList())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel_server = GrpcChannel.ForAddress(s.getURL());
                GStoreServices.GStoreServicesClient server_client = new GStoreServices.GStoreServicesClient(channel_server);
                server_client.StatusServerAsync(new AskStatusServer { });
            }
        }
    }
}