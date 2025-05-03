using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using static System.Net.Mime.MediaTypeNames;

namespace LaleMapTest
{
    //2025 ref ^ retrojen.org

    public struct AmigaPic
    {
        public Color[] palette;       // 32 girdilik palet
        public Color[] BackupPalette; // 32 girdilik palet

        public int width;             // Genişlik (dosyada bayt cinsinden)
        public int lumph;            // Yükseklik (satır sayısı)
        public int lumps;             // Linelumps (ll)
        public int dlumps;            // Bitplane sayısı (d)
        public byte[] pixelData;      // MakePalette'de kopyalanan sıkıştırılmış veri (dosyanın o+24'ten sonrası)
        public byte[] uncompressedData; // Çözülen ham piksel verisi

        // Aşağıdaki iki alan, RLE dekompresyonunda ihtiyaç duyulan kontrol veri bloklarının,
        // pic.pixelData içindeki başlangıç offsetlerini temsil eder.
        public int rleDataOffset;
        public int pointsOffset;

        public byte[,] indexedImage; //actual amiga image WxH
        public string error;
    }

    public struct LaleMap
    {
        public int startx;
        public int starty;


        public int width;             // Genişlik (dosyada bayt cinsinden)
        public int height;            // Yükseklik (satır sayısı)
        public string scenario;
    }

    public partial class Form1 : Form
    {
        private AmigaPic PicBank = new AmigaPic();
        Color[] BackupPalette1 = new Color[]
        {
            Color.FromArgb(0x00, 0x00, 0x00),    // COLOR00 = 000
            Color.FromArgb(0x00, 0x55, 0x66),     // COLOR01 = 056
            Color.FromArgb(0xCC, 0x22, 0x00),     // COLOR02 = C20
            Color.FromArgb(0x88, 0x77, 0x00),      // COLOR03 = 870
            Color.FromArgb(0x44, 0x33, 0x00),     // COLOR04 = 430
            Color.FromArgb(0x77, 0x66, 0x00),     // COLOR05 = 760
            Color.FromArgb(0x55, 0x44, 0x00),     // COLOR06 = 540
            Color.FromArgb(0x66, 0x55, 0x00),     // COLOR07 = 650
            Color.FromArgb(0x00, 0x66, 0x77),     // COLOR08 = 067
            Color.FromArgb(0x00, 0x77, 0x88),     // COLOR09 = 078
            Color.FromArgb(0x00, 0x88, 0x99),     // COLOR10 = 089
            Color.FromArgb(0x00, 0x33, 0x44),     // COLOR11 = 034
            Color.FromArgb(0x00, 0x44, 0x55),     // COLOR12 = 045
            Color.FromArgb(0x00, 0x55, 0x66),     // COLOR13 = 056
            Color.FromArgb(0x00, 0x44, 0x55),     // COLOR14 = 045
            Color.FromArgb(0x00, 0x33, 0x44),     // COLOR15 = 034
            Color.FromArgb(0x00, 0x55, 0x00),     // COLOR16 = 050
            Color.FromArgb(0x00, 0x66, 0x00),     // COLOR17 = 060
            Color.FromArgb(0x00, 0x44, 0x00),     // COLOR18 = 040
            Color.FromArgb(0xFF, 0xCC, 0xAA),      // COLOR19 = FCA
            Color.FromArgb(0x33, 0x33, 0x33),     // COLOR20 = 333
            Color.FromArgb(0x44, 0x44, 0x44),     // COLOR21 = 444
            Color.FromArgb(0x55, 0x55, 0x55),     // COLOR22 = 555
            Color.FromArgb(0x66, 0x66, 0x66),     // COLOR23 = 666
            Color.FromArgb(0x77, 0x77, 0x77),     // COLOR24 = 777
            Color.FromArgb(0x88, 0x88, 0x88),     // COLOR25 = 888
            Color.FromArgb(0x44, 0x44, 0x44),     // COLOR26 = 444
            Color.FromArgb(0x00, 0x33, 0x33),     // COLOR27 = 033
            Color.FromArgb(0x00, 0x55, 0x66),     // COLOR28 = 056
            Color.FromArgb(0x44, 0x66, 0x88),     // COLOR29 = 468
            Color.FromArgb(0xEE, 0x33, 0x00),     // COLOR30 = E30
            Color.FromArgb(0x99, 0x22, 0x00)      // COLOR31 = 920
        };
        Color[] BackupPalette2 = new Color[]
        {
            Color.FromArgb(0x00, 0x00, 0x00),    // COLOR00 = 000
            Color.FromArgb(0x00, 0x55, 0x00),    // COLOR01 = 050
            Color.FromArgb(0x00, 0x44, 0x00),    // COLOR02 = 040
            Color.FromArgb(0x00, 0x33, 0x00),    // COLOR03 = 030
            Color.FromArgb(0x00, 0x22, 0x88),    // COLOR04 = 028
            Color.FromArgb(0x00, 0x11, 0x66),    // COLOR05 = 016
            Color.FromArgb(0x00, 0x00, 0x44),    // COLOR06 = 004
            Color.FromArgb(0x55, 0x22, 0x00),    // COLOR07 = 520
            Color.FromArgb(0x44, 0x11, 0x00),    // COLOR08 = 410
            Color.FromArgb(0x33, 0x00, 0x00),    // COLOR09 = 300
            Color.FromArgb(0x55, 0x55, 0x55),    // COLOR10 = 555
            Color.FromArgb(0x44, 0x44, 0x44),    // COLOR11 = 444
            Color.FromArgb(0x33, 0x33, 0x33),    // COLOR12 = 333
            Color.FromArgb(0x22, 0x22, 0x22),    // COLOR13 = 222
            Color.FromArgb(0x11, 0x11, 0x11),    // COLOR14 = 111
            Color.FromArgb(0x66, 0x66, 0x66),    // COLOR15 = 666
            Color.FromArgb(0x00, 0x00, 0x00),    // COLOR16 = 000
            Color.FromArgb(0x22, 0x00, 0x22),    // COLOR17 = 202
            Color.FromArgb(0x44, 0x00, 0x55),    // COLOR18 = 405
            Color.FromArgb(0x77, 0x00, 0x88),    // COLOR19 = 708
            Color.FromArgb(0x00, 0x66, 0x00),    // COLOR20 = 060
            Color.FromArgb(0x00, 0x00, 0x00),    // COLOR21 = 000
            Color.FromArgb(0x66, 0x33, 0x00),    // COLOR22 = 630
            Color.FromArgb(0x11, 0x00, 0x00),    // COLOR23 = 100
            Color.FromArgb(0x00, 0x33, 0x99),    // COLOR24 = 039
            Color.FromArgb(0x33, 0x00, 0x00),    // COLOR25 = 300
            Color.FromArgb(0x55, 0x11, 0x11),    // COLOR26 = 511
            Color.FromArgb(0x77, 0x22, 0x22),    // COLOR27 = 722
            Color.FromArgb(0x88, 0x88, 0x88),    // COLOR28 = 888
            Color.FromArgb(0xCC, 0x00, 0x00),    // COLOR29 = C00
            Color.FromArgb(0x77, 0x00, 0x00),    // COLOR30 = 700
            Color.FromArgb(0x77, 0x77, 0x77)     // COLOR31 = 777
        };
        Color[] BackupPalette3 = new Color[]
        {
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR01 = FF8
            Color.FromArgb(0xFF, 0xff, 0x88), // COLOR01 = FF8
            Color.FromArgb(0x22, 0x22, 0x00), // COLOR02 = 220
            Color.FromArgb(0x33, 0x33, 0x00), // COLOR03 = 330
            Color.FromArgb(0x44, 0x44, 0x00), // COLOR04 = 440
            Color.FromArgb(0x55, 0x55, 0x11), // COLOR05 = 551
            Color.FromArgb(0x66, 0x66, 0x11), // COLOR06 = 661
            Color.FromArgb(0x77, 0x77, 0x11), // COLOR07 = 771
            Color.FromArgb(0x88, 0x88, 0x22), // COLOR08 = 882
            Color.FromArgb(0x99, 0x99, 0x22), // COLOR09 = 992
            Color.FromArgb(0xAA, 0xaa, 0x22), // COLOR10 = AA3
            Color.FromArgb(0xBB, 0xbb, 0x44), // COLOR11 = BB4
            Color.FromArgb(0xCC, 0xcc, 0x44), // COLOR12 = CC4
            Color.FromArgb(0xDD, 0xdd, 0x00), // COLOR13 = DD5
            Color.FromArgb(0xEE, 0xee, 0x66), // COLOR14 = EE6
            Color.FromArgb(0x11, 0x11, 0x00), // COLOR15 = 110
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR16 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR17 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR18 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR19 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR20 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR21 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR22 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR23 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR24 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR25 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR26 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR27 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR28 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR29 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR30 = 000
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR31 = 000
};
        Color[] BackupPalette = new Color[]
{
    Color.FromArgb(0xff, 0x00, 0xff), // COLOR00 = 000
    Color.FromArgb(0x55, 0x11, 0x11), // COLOR01 = 511
    Color.FromArgb(0x77, 0x22, 0x22), // COLOR02 = 722
    Color.FromArgb(0x99, 0x33, 0x33), // COLOR03 = 933
    Color.FromArgb(0x88, 0x77, 0x66), // COLOR04 = 876
    Color.FromArgb(0x00, 0x33, 0x77), // COLOR05 = 037
    Color.FromArgb(0x00, 0x22, 0x55), // COLOR06 = 025
    Color.FromArgb(0x00, 0x11, 0x33), // COLOR07 = 013
    Color.FromArgb(0x55, 0x33, 0x00), // COLOR08 = 530
    Color.FromArgb(0x88, 0x66, 0x00), // COLOR09 = 860
    Color.FromArgb(0x33, 0x22, 0x00), // COLOR10 = 320
    Color.FromArgb(0x77, 0x66, 0x55), // COLOR11 = 765
    Color.FromArgb(0x66, 0x55, 0x44), // COLOR12 = 654
    Color.FromArgb(0x55, 0x44, 0x33), // COLOR13 = 543
    Color.FromArgb(0x44, 0x33, 0x22), // COLOR14 = 432
    Color.FromArgb(0x33, 0x22, 0x11), // COLOR15 = 321
    Color.FromArgb(0x00, 0x00, 0x00), // COLOR16 = 000
    Color.FromArgb(0xAA, 0x77, 0x00), // COLOR17 = A70
    Color.FromArgb(0x77, 0x55, 0x00), // COLOR18 = 750
    Color.FromArgb(0x66, 0x44, 0x00), // COLOR19 = 640
    Color.FromArgb(0x00, 0x77, 0x00), // COLOR20 = 070
    Color.FromArgb(0x44, 0x33, 0x00), // COLOR21 = 430
    Color.FromArgb(0x00, 0x22, 0x00), // COLOR22 = 020
    Color.FromArgb(0x00, 0x88, 0x00), // COLOR23 = 080
    Color.FromArgb(0x55, 0x00, 0x00), // COLOR24 = 500
    Color.FromArgb(0x99, 0x00, 0x00), // COLOR25 = 900
    Color.FromArgb(0x22, 0x11, 0x00), // COLOR26 = 210
    Color.FromArgb(0xDD, 0x00, 0x00), // COLOR27 = D00
    Color.FromArgb(0x00, 0x44, 0x00), // COLOR28 = 040
    Color.FromArgb(0x00, 0x55, 0x00), // COLOR29 = 050
    Color.FromArgb(0x00, 0x66, 0x00), // COLOR30 = 060
    Color.FromArgb(0xBB, 0xBB, 0xBB), // COLOR31 = BBB
}; //ingame palette
        Color[] BackupPalette5 = new Color[]  
        {
            Color.FromArgb(0xff, 0x00, 0xff), // COLOR00 = 000
            Color.FromArgb(0x00, 0x55, 0x00), // COLOR01 = 050
            Color.FromArgb(0x00, 0x44, 0x00), // COLOR02 = 040
            Color.FromArgb(0x00, 0x33, 0x00), // COLOR03 = 030
            Color.FromArgb(0x00, 0x22, 0x88), // COLOR04 = 028
            Color.FromArgb(0x00, 0x11, 0x66), // COLOR05 = 016
            Color.FromArgb(0x00, 0x00, 0x44), // COLOR06 = 004
            Color.FromArgb(0x55, 0x22, 0x00), // COLOR07 = 520
            Color.FromArgb(0x44, 0x11, 0x00), // COLOR08 = 410
            Color.FromArgb(0x33, 0x00, 0x00), // COLOR09 = 300
            Color.FromArgb(0x55, 0x55, 0x55), // COLOR10 = 555
            Color.FromArgb(0x44, 0x44, 0x44), // COLOR11 = 444
            Color.FromArgb(0x33, 0x33, 0x33), // COLOR12 = 333
            Color.FromArgb(0x22, 0x22, 0x22), // COLOR13 = 222
            Color.FromArgb(0x11, 0x11, 0x11), // COLOR14 = 111
            Color.FromArgb(0x66, 0x66, 0x66), // COLOR15 = 666
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR16 = 000
            Color.FromArgb(0x22, 0x00, 0x22), // COLOR17 = 202
            Color.FromArgb(0x44, 0x00, 0x55), // COLOR18 = 405
            Color.FromArgb(0x77, 0x00, 0x88), // COLOR19 = 708
            Color.FromArgb(0x00, 0x66, 0x00), // COLOR20 = 060
            Color.FromArgb(0x00, 0x00, 0x00), // COLOR21 = 000
            Color.FromArgb(0x66, 0x33, 0x00), // COLOR22 = 630
            Color.FromArgb(0x11, 0x00, 0x00), // COLOR23 = 100
            Color.FromArgb(0x00, 0x33, 0x99), // COLOR24 = 039
            Color.FromArgb(0x33, 0x00, 0x00), // COLOR25 = 300
            Color.FromArgb(0x55, 0x11, 0x11), // COLOR26 = 511
            Color.FromArgb(0x77, 0x22, 0x22), // COLOR27 = 722
            Color.FromArgb(0x88, 0x88, 0x88), // COLOR28 = 888
            Color.FromArgb(0xCC, 0x00, 0x00), // COLOR29 = C00
            Color.FromArgb(0x77, 0x00, 0x00), // COLOR30 = 700
            Color.FromArgb(0x77, 0x77, 0x77), // COLOR31 = 777
        };  //Battle palette


        Dictionary<ushort, string> LevelNames = new Dictionary<ushort, string>
                    {
                        { 0,   "Boş" },
                        { 1,   "Otopark" },
                        { 2,   "Mecidiyeköy" },
                        { 3,   "Beşiktaş" },
                        { 4,   "Eminönü" },
                        { 5,   "Üsküdar" },
                        { 6,   "???" },
                        { 7,   "Kadıköy" },
                        { 8,   "Saray Önü" },
                        { 9,   "???" },
                        { 10,   "???" },
                        { 11,   "Savaş Girişi" },
                        { 12,   "Sarıyer" },
                        { 13,   "Kanalizasyon x" },
                        { 14,   "???" },
                        { 15,   "Topkapı Sarayı" },
                        { 16,   "Sarıyer Kanalizasyonu" },
                        { 17,   "Kız Kulesi" },
                        { 18,   "Savaş" }
                    };


        private LaleMap map = new LaleMap();
        private int searchindex = 0;
        List<byte[]> eventDataList = new List<byte[]>(); // form sınıfına ekle
        int eventOffset = 0;


        public struct ClickedRoomInfo
        {
            public int index; // ListBox oda indexi
            public int wall;  // 1=üst, 2=sağ, 3=alt, 4=sol, 5=orta
        }

        private int roomSize = 15;
        private int margin = 1;
        Bitmap dungeonMapBackup = null; // Orijinal haritanın kopyası
        Rectangle? lastHighlightRect = null; // Son restore edilecek alan


        private byte[] laleExe;

        //editor data
        private int highlightRoomIndex = -1;
        private int highlightWallDirection = -1;




        string[] items = {
            "maus",
            "zopa",
            "sivri daS",
            "kIsa zopa",
            "lale",
            "SiSe",
            "levye",
            "levrek",
            "cetvel",
            "t-cetveli",
            "mezura",
            "galvanometre",
            "terlik",
            "baCemak",
            "kIrIk SiSe",
            "tornavida",
            "yIldIz torn.",
            "ing. anahtarI",
            "fra. anahtarI",
            "kazma",
            "Catal",
            "DaS",
            "tUftUf",
            "sapan",
            "tebeSir",
            "fIstIk",
            "COtek"
        };

        string[] enemies = {
        "Player", //0
        "irospa",
        "ipna",
        "lavuk",
        "maganda",
        "hIrgIz",
        "bahCIvan",
        "lale savaSCIsI",
        "dadaS",
        "meycik yuzIr",
        "faytIr", //10
        "kIlerik",
        "ayI",
        "kOpek",
        "guS",
        "fare",
        "yarasa",
        "OmUrcek bOcUGU",
        "dev OmUrcek",
        "yobaz ograten",
        "yobaz beSamel", //20
        "yobaz bolonez",
        "yobaz napoliten",
        "kelp",
        "karafatma",
        "Cembersakal",
        "fanatik",
        "leS gargasI",
        "dellenmiS OGrenci",
        "dellenmiS OGretmen",
        "balgam", //30
        "afakan",
        "barut husam",
        "hasmet maap",
        "cehalet",
        "kIl sItkI",
        "kIl sItkI",
        "dragon"
    };


        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filename = "haritalar\\" + textBox1.Text;
            // Dosya var mı kontrol et
            if (!File.Exists(filename))
                return;
            LoadAbkPic(filename);

            map = ListData(PicBank);

            eventOffset = drawDungeon() - 1;
            textBox2.Text = "Level:" + textBox1.Text + "\r\n";
            byte[] result = File.ReadAllBytes("senaryo\\" + textBox1.Text);
            listEvents(result, eventOffset);

            ParseMapHeader(ParseHeaderString(listBox1.Items[0].ToString()));
        }

        public void drawRoom(int selectedListIndex)
        {
            if (selectedListIndex < 0 || selectedListIndex >= listBox1.Items.Count)
                return;

            string selectedLine = listBox1.Items[selectedListIndex].ToString();
            string[] parts = selectedLine.Split('\t'); // Veriyi parçala

            if (parts.Length < 9) // En az 9 parça olmalı (Idx + 4 atlanan + 4 duvar)
                return;

            // İlk 4 değeri atlıyoruz, sonraki 4 değer duvarları belirliyor
            int topWall = int.Parse(parts[6]);
            int rightWall = int.Parse(parts[8]);
            int bottomWall = int.Parse(parts[10]);
            int leftWall = int.Parse(parts[12]);

            // PictureBox çizimi için
            int roomSize = 100; // Kare boyutu
            int margin = 10; // Kenar boşluğu
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                Pen pen = new Pen(Color.Black, 3);

                int x = margin;
                int y = margin;

                // Duvarları çiz
                if (topWall != 0)
                    g.DrawLine(pen, x, y, x + roomSize, y); // Üst duvar
                if (rightWall != 0)
                    g.DrawLine(pen, x + roomSize, y, x + roomSize, y + roomSize); // Sağ duvar
                if (bottomWall != 0)
                    g.DrawLine(pen, x, y + roomSize, x + roomSize, y + roomSize); // Alt duvar
                if (leftWall != 0)
                    g.DrawLine(pen, x, y, x, y + roomSize); // Sol duvar
            }

            pictureBox1.Image = bmp; // Çizimi PictureBox'a aktar
        }
        private Brush GetFloorBrush(int floorType)
        {
            switch (floorType)
            {
                case 1: return Brushes.LightGray;   // Taş Zemin
                case 2: return Brushes.Brown;       // Ahşap Zemin
                case 3: return Brushes.DarkGray;    // bilmediğim zemin
                case 4: return Brushes.SandyBrown;  // bozuk ahşap Zemin
                default: return Brushes.Orange;      // Varsayılan (Boş)
            }
        }


        private Brush GetCeilingBrush(int ceilingType)
        {
            switch (ceilingType)
            {
                case 1: return Brushes.Black;
                case 2: return Brushes.Blue;
                case 3: return Brushes.Green;
                case 4: return Brushes.Red;
                default: return Brushes.DarkSlateGray;
            }
        }


        public ClickedRoomInfo GetRoomAndWallFromPixel(int px, int py)
        {
            ClickedRoomInfo info = new ClickedRoomInfo { index = -1, wall = -1 };

            int roomAndMargin = roomSize + margin;
            int cellX = px / roomAndMargin;
            int cellY = py / roomAndMargin;

            int insideX = px % roomAndMargin;
            int insideY = py % roomAndMargin;

            if (insideX >= roomSize || insideY >= roomSize) return info;
            if (cellX >= map.width || cellY >= map.height) return info;

            int index = cellY * map.width + cellX + 1; // +1: listBox1'deki offset

            // Oda içindeki konum (merkez 0,0)
            float cx = roomSize / 2f;
            float cy = roomSize / 2f;
            float dx = insideX - cx;
            float dy = insideY - cy;

            int wall;
            if (Math.Abs(dx) < roomSize * 0.3 && Math.Abs(dy) < roomSize * 0.3)
                wall = 5; // orta
            else if (Math.Abs(dx) > Math.Abs(dy))
                wall = dx > 0 ? 2 : 4; // sağ veya sol
            else
                wall = dy > 0 ? 3 : 1; // alt veya üst

            info.index = index;
            info.wall = wall;

            return info;
        }


        public int GetRoomIndexFromPixel(int px, int py)
        {
            int roomAndMargin = roomSize + margin;

            // Hücre koordinatını bul
            int cellX = px / roomAndMargin;
            int cellY = py / roomAndMargin;

            // Tıklanan noktanın margin içinde mi olduğunu kontrol et
            int insideX = px % roomAndMargin;
            int insideY = py % roomAndMargin;

            // Eğer margin boşluğa denk geldiyse -1 döndür
            if (insideX >= roomSize || insideY >= roomSize)
                return -1;

            // Map sınırları içinde mi kontrolü
            if (cellX >= map.width || cellY >= map.height)
                return -1;

            // Oda indexini döndür
            return cellY * map.width + cellX + 1; // +1 çünkü listBox1'de 0. satır genelde başlık
        }


        public int drawDungeon(int highlightEvent = -1)
        {
            int minEvent = 999999999;
            if (listBox1.Items.Count < map.width * map.height) // En az 100 oda olmalı
            {
                MessageBox.Show("Eksik oda verisi!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (0);
            }
            roomSize = 15;
            // Dungeon boyutları
            int margin = 1; // Oda araları boşluk

            int totalRoomWidth = map.width;
            int totalRoomHeight = map.height;

            // Kaç adet boşluk (margin) olacak? Oda sayısından 1 fazla
            int totalMarginX = (map.width + 1) * margin;
            int totalMarginY = (map.height + 1) * margin;

            // Mevcut kullanılabilir alanlar
            int availableWidth = pictureBox1.Width - totalMarginX;
            int availableHeight = pictureBox1.Height - totalMarginY;

            // Hem X hem Y için uygun en büyük kare boyutu
            roomSize = Math.Min(availableWidth / totalRoomWidth, availableHeight / totalRoomHeight);



            int dungeonSizeW = map.width;
            int dungeonSizeH = map.height;
            // Resmi oluştur
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                //g.Clear(Color.White);
                g.FillRectangle(Brushes.LightGray, 0, 0, map.width * (margin + roomSize), (margin + roomSize) * map.height);
                Font font = new Font("Arial", 6, FontStyle.Bold);
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                for (int i = 0; i < dungeonSizeH; i++)
                {
                    for (int j = 0; j < dungeonSizeW; j++)
                    {
                        int index = i * dungeonSizeW + j;
                        if (index + 1 >= listBox1.Items.Count) continue;

                        string selectedLine = listBox1.Items[index + 1].ToString();
                        string[] parts = selectedLine.Split('\t'); // Veriyi parçala

                        if (parts.Length < 15) continue; // Yeterli veri olup olmadığını kontrol et

                        // Zemin ve tavan bilgileri
                        int floorType = int.Parse(parts[13]);  // Zemin tipi
                        int ceilingType = int.Parse(parts[14]); // Tavan tipi

                        // Duvarın tipini belirleyen verileri al
                        int topType = int.Parse(parts[5]);
                        int rightType = int.Parse(parts[7]);
                        int bottomType = int.Parse(parts[9]);
                        int leftType = int.Parse(parts[11]);

                        // Duvarın olup olmadığını belirleyen verileri al
                        int topWall = int.Parse(parts[6]);
                        int rightWall = int.Parse(parts[8]);
                        int bottomWall = int.Parse(parts[10]);
                        int leftWall = int.Parse(parts[12]);

                        int roomevent = int.Parse(parts[19]);
                        int roomevent32 = int.Parse(parts[18]);
                        int roomevent64 = int.Parse(parts[17]);
                        int enemySpawn = int.Parse(parts[15]); //random spawn
                        int isShop = int.Parse(parts[16]); //dükkanlar

                        // Odanın x, y konumunu belirle
                        int x = j * (roomSize + margin);
                        int y = i * (roomSize + margin);

                        // **1. Zemin Tipine Göre Odanın Arkasını Doldur**
                        Brush floorBrush = GetFloorBrush(ceilingType);
                        // **2. Duvarları Çiz**

                        if (highlightEvent == (roomevent + (roomevent32 - 1) * 31))
                            g.FillRectangle(Brushes.Magenta, x, y, roomSize, roomSize);
                        else if (isShop == 31)
                            g.FillRectangle(Brushes.RoyalBlue, x, y, roomSize, roomSize);
                        else
                            g.FillRectangle(floorBrush, x, y, roomSize, roomSize);

                        DrawWall(g, x, y, x + roomSize, y, topWall, topType, x, y); // Üst duvar
                        DrawWall(g, x + roomSize, y, x + roomSize, y + roomSize, rightWall, rightType, x, y); // Sağ duvar
                        DrawWall(g, x, y + roomSize, x + roomSize, y + roomSize, bottomWall, bottomType, x, y); // Alt duvar
                        DrawWall(g, x, y, x, y + roomSize, leftWall, leftType, x, y); // Sol duvar

                        // **3. Tavan Tipine Göre Oda İndeks Rengini Ayarla**
                        Brush textBrush = GetCeilingBrush(floorType);
                        Brush eventBrush = Brushes.Yellow;

                        // **4. Oda Index'ini Ortaya Yaz**

                        g.DrawString((index + 1).ToString(), font, textBrush, new RectangleF(x, y, roomSize, roomSize), sf);
                        if (roomevent > 0)
                        {
                            g.DrawString((roomevent + (roomevent32 - 1) * 31).ToString(), font, eventBrush, new RectangleF(x, y - 10, roomSize, roomSize), sf);
                            if (minEvent > (roomevent + (roomevent32 - 1) * 31)) minEvent = (roomevent + (roomevent32 - 1) * 31);
                        }
                        string enemies = "";
                        if (enemySpawn > 0)
                        {
                            enemies = "⚔" + enemySpawn.ToString().Trim();
                            g.DrawString(enemies, font, Brushes.Red, new RectangleF(x, y + 10, roomSize, roomSize), sf);
                        }
                    }
                }
            }

            pictureBox1.Image = bmp; // Çizimi PictureBox'a aktar

            // Haritayı yedekle (bir kere çizim sonrası)
            dungeonMapBackup?.Dispose();
            dungeonMapBackup = (Bitmap)bmp.Clone();

            return minEvent;
        }

        public void updateWallWithNeighbor(ClickedRoomInfo info, int newWallExists, int newWallType)
        {
            // Mevcut odayı güncelle
            string oldLine = listBox1.Items[info.index].ToString();
            string newLine = updateWallLine(oldLine, info.wall, newWallExists, newWallType);
            listBox1.Items[info.index] = newLine;

            // Komşu oda varsa, onu da güncelle
            var opp = findOppositeWall(info);
            if (opp.index > 0 && opp.wall > 0 && opp.index < listBox1.Items.Count)
            {
                string oldLine2 = listBox1.Items[opp.index].ToString();
                string newLine2 = updateWallLine(oldLine2, opp.wall, newWallExists, newWallType);
                listBox1.Items[opp.index] = newLine2;
            }
        }


        public string updateWallLine(string line, int wallDirection, int newWallExists, int newWallType)
        {
            string[] parts = line.Split('\t');

            if (parts.Length < 20) return line; // Geçersiz satır

            int typeIndex = -1;
            int existsIndex = -1;

            switch (wallDirection)
            {
                case 1: typeIndex = 5; existsIndex = 6; break; // üst
                case 2: typeIndex = 7; existsIndex = 8; break; // sağ
                case 3: typeIndex = 9; existsIndex = 10; break; // alt
                case 4: typeIndex = 11; existsIndex = 12; break; // sol
                default: return line;
            }

            parts[typeIndex] = newWallType.ToString();
            parts[existsIndex] = newWallExists.ToString();

            return string.Join("\t", parts);
        }


        public void updateRoomHighlight(int roomIndex, int wallDirection)
        {
            if (dungeonMapBackup == null || roomIndex < 1 || roomIndex > map.width * map.height)
                return;

            // Önceki highlight'ı geri al
            if (lastHighlightRect.HasValue)
            {
                using (Graphics g = Graphics.FromImage(pictureBox1.Image))
                {
                    g.DrawImage(dungeonMapBackup, lastHighlightRect.Value, lastHighlightRect.Value, GraphicsUnit.Pixel);
                }
                lastHighlightRect = null;
            }

            int index = roomIndex - 1;
            int row = index / map.width;
            int col = index % map.width;

            int x = col * (roomSize + margin);
            int y = row * (roomSize + margin);

            int lineWidth = Math.Max(3, roomSize / 5);
            int oversize = lineWidth + 2; // ekstra güvenli alan
            Rectangle MarkerroomRect = new Rectangle(x + 2, y + 2, roomSize - 4, roomSize - 4);
            Rectangle roomRect = new Rectangle(
                x - oversize,
                y - oversize,
                roomSize + oversize * 2,
                roomSize + oversize * 2);

            lastHighlightRect = roomRect;

            using (Pen pen = new Pen(Color.Red, lineWidth))
            {
                pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;
                using (Graphics g = Graphics.FromImage(pictureBox1.Image))
                {
                    switch (wallDirection)
                    {
                        case 1: g.DrawLine(pen, x, y, x + roomSize, y); break; // üst
                        case 2: g.DrawLine(pen, x + roomSize, y, x + roomSize, y + roomSize); break; // sağ
                        case 3: g.DrawLine(pen, x, y + roomSize, x + roomSize, y + roomSize); break; // alt
                        case 4: g.DrawLine(pen, x, y, x, y + roomSize); break; // sol
                        case 5:
                            using (Brush brush = new SolidBrush(Color.FromArgb(100, Color.Red)))
                            {
                                g.FillRectangle(brush, MarkerroomRect);
                            }
                            break;
                    }
                }
            }

            pictureBox1.Invalidate(roomRect);
        }

        public ClickedRoomInfo findOppositeWall(ClickedRoomInfo roomInfo)
        {
            ClickedRoomInfo opposite = new ClickedRoomInfo { index = -1, wall = -1 };

            if (roomInfo.wall < 1 || roomInfo.wall > 4) return opposite; // sadece 1–4 duvarlar için geçerli

            int roomId = roomInfo.index;
            Point p = RoomCoordsFromId(roomId);
            int col = p.X;
            int row = p.Y;

            int oppositeCol = col;
            int oppositeRow = row;
            int oppositeWall = -1;

            switch (roomInfo.wall)
            {
                case 1: oppositeRow = row - 1; oppositeWall = 3; break; // üst → alt
                case 2: oppositeCol = col + 1; oppositeWall = 4; break; // sağ → sol
                case 3: oppositeRow = row + 1; oppositeWall = 1; break; // alt → üst
                case 4: oppositeCol = col - 1; oppositeWall = 2; break; // sol → sağ
            }

            if (oppositeCol < 0 || oppositeRow < 0 || oppositeCol >= map.width || oppositeRow >= map.height)
                return opposite; // sınır dışı

            int oppositeIndex = oppositeRow * map.width + oppositeCol + 1;

            opposite.index = oppositeIndex;
            opposite.wall = oppositeWall;
            return opposite;
        }


        private Point RoomCoordsFromId(int roomId)
        {
            int index = roomId - 1;
            int row = index / map.width;
            int col = index % map.width;
            return new Point(col, row);
        }


        private void DrawWall(Graphics g, int x1, int y1, int x2, int y2, int wallExists, int wallType, int xc, int yc)
        {
            if (wallExists == 0) return;

            Pen pen = new Pen(Color.Black, 2);
            Pen duvar = new Pen(Color.Black, 3);
            switch (wallExists)
            {
                case 1:
                    Pen pene = new Pen(Color.Blue, 1);
                    g.DrawLine(pene, x1, y1, x2, y2);
                    break;
                case 2:
                    // "Kapı" görselliği
                    pen.Color = Color.Red;

                    // Kapının yönünü belirle
                    bool isVertical = (x1 == x2);
                    bool isHorizontal = (y1 == y2);

                    // Kapı uzunluğu (çizgi uzunluğu)
                    int dx = x2 - x1;
                    int dy = y2 - y1;

                    // Çizgiyi parçalara bölelim: iki kısa çizgi, arada boşluk olacak
                    if (isHorizontal)
                    {
                        int length = dx;
                        int gap = Math.Abs(length) / 4;

                        // Sol çizgi
                        g.DrawLine(duvar, x1, y1, x1 + length / 2 - gap, y1);
                        // Sağ çizgi
                        g.DrawLine(duvar, x1 + length / 2 + gap, y1, x2, y2);
                        g.DrawLine(pen, x1 + length / 2 - gap, y1, x1 + length / 2 + gap, y1);
                    }
                    else if (isVertical)
                    {
                        int length = dy;
                        int gap = Math.Abs(length) / 4;

                        // Üst çizgi
                        g.DrawLine(duvar, x1, y1, x1, y1 + length / 2 - gap);
                        // Alt çizgi
                        g.DrawLine(duvar, x1, y1 + length / 2 + gap, x2, y2);

                        g.DrawLine(pen, x1, y1 + length / 2 - gap, x1, y1 + length / 2 + gap);

                    }
                    else
                    {
                        // Kapı eğikse, düz çiz
                        g.DrawLine(pen, x1, y1, x2, y2);
                    }
                    break;

                case 3:
                    pen.Color = Color.Green; // Başka tür bir kapıysa düz çizgi
                    g.DrawLine(pen, x1, y1, x2, y2);
                    break;

                case 4:
                    pen.Color = Color.Cyan;
                    g.DrawLine(pen, x1, y1, x2, y2);
                    break;

                default:
                    pen.Color = Color.Magenta;
                    g.DrawLine(pen, x1, y1, x2, y2);
                    break;
            }

            switch (wallType)
            {
                case 4:
                    Pen tex = new Pen(Color.Cyan, 1);
                    //g.DrawLine(tex, x1, y1, x2, y2);
                    DrawPlus(g, tex, x1, y1, x2, y2);

                    break;
            }
        }

        private void DrawPlus(Graphics g, Pen pen, int x1, int y1, int x2, int y2)
        {
            // Orta noktayı bul
            float midX = (x1 + x2) / 2f;
            float midY = (y1 + y2) / 2f;

            // Çizginin yön vektörü
            float dx = x2 - x1;
            float dy = y2 - y1;

            // Uzunluğu hesapla
            float length = (float)Math.Sqrt(dx * dx + dy * dy);

            if (length == 0) return; // Geçersiz çizgi

            // Normalize et
            float nx = dx / length;
            float ny = dy / length;

            // Dik vektör = (-ny, nx)
            float px = -ny;
            float py = nx;

            // Yarı uzunluklu dik çizgi için uzunluk
            float halfLen = length / 2f;

            // Başlangıç ve bitiş noktaları
            float perpX1 = midX - px * halfLen / 2f;
            float perpY1 = midY - py * halfLen / 2f;
            float perpX2 = midX + px * halfLen / 2f;
            float perpY2 = midY + py * halfLen / 2f;

            // İlk çizgi (verilen)
            g.DrawLine(pen, x1, y1, x2, y2);

            // Ortasına dik çizgi
            g.DrawLine(pen, perpX1, perpY1, perpX2, perpY2);
        }


        public void readData(string filename)
        {
            try
            {
                listBox1.Items.Clear(); // Listeyi temizle

                if (!File.Exists(filename))
                {
                    MessageBox.Show("Dosya bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string[] lines = File.ReadAllLines(filename);
                foreach (string line in lines)
                {
                    if (line.StartsWith("Idx:"))
                    {
                        listBox1.Items.Add(line.Trim()); // Satırı listeye ekle
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dosya okunurken hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Story
        private void listEvents(byte[] storyData, int eOffset)
        {
            listBox2.Items.Clear();
            eventDataList.Clear();


            if (storyData == null || storyData.Length < 22)
            {
                listBox2.Items.Add("Veri geçersiz veya eksik.");
                return;
            }

            int offset = 20; // İlk 20 byte: Amos data bank header

            // kaç event var?
            ushort eventCount = ReadWordBigEndian(storyData, offset);
            offset += 2;

            for (int i = 0; i < eventCount; i++)
            {
                if (offset + 2 > storyData.Length)
                {
                    listBox2.Items.Add($"Event #{i + 1 + eOffset}: Başlık okunamıyor (veri bitti)");
                    break;
                }

                ushort eventLength = ReadWordBigEndian(storyData, offset);
                offset += 2;

                if (offset + eventLength > storyData.Length)
                {
                    listBox2.Items.Add($"Event #{i + 1 + eOffset}: Veri eksik (uzunluk={eventLength})");
                    break;
                }

                byte[] eventData = new byte[eventLength];
                Array.Copy(storyData, offset, eventData, 0, eventLength);
                offset += eventLength;

                string eventText = DecipherROT(eventData);

                eventDataList.Add(eventData);

                listBox2.Items.Add($"Event #{i + 1 + eOffset} ({eventLength} byte)");

            }
            return;
        }

        private ushort ReadWordBigEndian(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }



        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                // drawRoom(listBox1.SelectedIndex);
                if (listBox1.SelectedIndex == 0)
                {
                    ParseMapHeader(ParseHeaderString(listBox1.Items[0].ToString()));
                }
            }
        }

        private int[] ParseHeaderString(string headerStr)
        {
            if (string.IsNullOrWhiteSpace(headerStr))
                return new int[0];

            return headerStr
                .Split('\t') // Tab karakterine göre ayır
                .Select(s =>
                {
                    if (int.TryParse(s.Trim(), out int val))
                        return val;
                    return -1; // Hatalıysa -1 döndür (istersen burada hata mesajı da ekleyebilirsin)
                })
                .Where(i => i >= 0) // Geçerli değerleri al
                .ToArray();
        }


        private void ParseMapHeader(int[] header)
        {
            if (header.Length < 32)
            {
                textBox2.Text = "Geçersiz header, 32 değer bekleniyor.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            string desc =  LevelNames.TryGetValue((ushort)Convert.ToInt32(textBox1.Text), out var d) ? d : "Bilinmeyen Level";
            sb.AppendLine("Bölüm " + textBox1.Text + ": " + desc);
            // 0-3: Bilinmiyor
            sb.AppendLine("0-3 : (bilinmeyen) " + string.Join(" ", header[0], header[1], header[2], header[3]));

            // 4: Genişlik
            sb.AppendLine("4 : Genişlik  = " + header[4]);

            // 5: Yükseklik
            sb.AppendLine("5 : Yükseklik = " + header[5]);

            // 6-12: Bilinmiyor
            sb.AppendLine("6-12 : (bilinmeyen) " + string.Join(" ", header.Skip(6).Take(7)));

            // 13-20: Düşman numaraları
            sb.AppendLine("13-20 : Süpriz Düşman Listesi");
            for (int i = 13; i <= 20; i++)
            {
                int enemyId = header[i];
                string name = (enemyId >= 0 && enemyId < enemies.Length) ? enemies[enemyId] : "???";
                sb.AppendLine($"  {i}: #{enemyId} - {name}");
            }

            // 21-24: Bilinmiyor
            eventOffset = header[24] + (header[23] - 1) * 31;
            sb.AppendLine("21-24 : (Event Offset) " + string.Join(" ", eventOffset.ToString())); //header.Skip(21).Take(4)));
            
            // 25: Bilinmiyor
            sb.AppendLine("25 : (bilinmeyen) " + header[25]);

            // 26-31: Bilinmiyor
            sb.AppendLine("26-31 : (bilinmeyen) " + string.Join(" ", header.Skip(26).Take(6)));

            textBox2.Text += sb.ToString();
        }

        public string describeRoom(string line)
        {
            string[] parts = line.Split('\t');
            if (parts.Length < 20) return "Geçersiz oda verisi.";

            try
            {
                int ceilingType = int.Parse(parts[13]);
                int floorType = int.Parse(parts[14]);

                int topType = int.Parse(parts[5]);
                int rightType = int.Parse(parts[7]);
                int bottomType = int.Parse(parts[9]);
                int leftType = int.Parse(parts[11]);

                int topWall = int.Parse(parts[6]);
                int rightWall = int.Parse(parts[8]);
                int bottomWall = int.Parse(parts[10]);
                int leftWall = int.Parse(parts[12]);

                int enemySpawn = int.Parse(parts[15]);
                int isShop = int.Parse(parts[16]);

                int roomevent64 = int.Parse(parts[17]);
                int roomevent32 = int.Parse(parts[18]);
                int roomevent = int.Parse(parts[19]);

                int eventIndex = ( roomevent + (roomevent32 - 1) * 31)- eventOffset;

                string desc = "";

                var idDescriptions = new Dictionary<ushort, string>
                    {
                        { 0,   "Boş" },
                        { 1,   "Ahşap" },
                        { 2,   "Tuğla-Demir" },
                        { 3,   "Taş-Ahşap" },
                        { 4,   "Ağaçlık" },
                        { 5,   "Kanalizasyon" },
                        { 6,   "Kemik" },
                        { 7,   "Sunta" },
                        { 8,   "Deniz" },
                        { 9,   "İniş/çıkış" },
                    };


                desc += $"\r\n- Tavan: {ceilingType}\r\n\r\n";

                desc += $"Doku Seti:\r\n";
                desc += $"- \tÜst: {topType},\r\n Sol: {leftType}\t\tSağ: {rightType}\r\n\tAlt: {bottomType}\r\n \r\n";

                desc += $"Fonsiyon:\r\n";
                desc += $"- \tÜst: {topWall},\r\n Sol: {leftWall}\t\tSağ: {rightWall}\r\n\tAlt: {bottomWall}\r\n \r\n";   

                desc += $"- Zemin: {floorType}\r\n";

                // Event bilgisi
                if (eventIndex >= 0 && eventIndex < eventDataList.Count)
                {
                    byte[] data = eventDataList[eventIndex];
                    string eventText = ViewEvent(data);
                    if (checkBox1.Checked) eventText += ViewEvent2(data);
                    desc += $"Oda Eventleri (#{eventIndex}):\r\n{eventText}\r\n\r\n";
                }

                // Özel durumlar
                if (isShop == 31)
                    desc += "🛒 Bu oda bir dükkandır.\r\n";
                if (enemySpawn == 15)
                    desc += "⚠️ Bu odada rastgele düşman spawn olabilir.\r\n";

                return desc;
            }
            catch
            {
                return "Oda verisi parse edilirken hata oluştu.";
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedIndex >= 0)
                drawDungeon(1 + listBox2.SelectedIndex + eventOffset);
            else
                drawDungeon();
        }
        private string lastfile = "";
        private string lastfilepath = "";
        private void button2_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Dialog ayarları
            openFileDialog.Title = "Bir dosya seçin";
            openFileDialog.Filter = "Tüm dosyalar (*.*)|*.*"; // İsteğe göre filtre ekleyebilirsiniz

            // Dialog penceresini göster ve kullanıcı OK'e basarsa
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                // Seçilen dosyayı oku
                string filename = openFileDialog.FileName;

                if (!File.Exists(filename))
                    return;
                lastfilepath = filename;

                lastfile = openFileDialog.SafeFileName;
                LoadAbkPic(filename);
            }


        }



        private void LoadAbkPic(string filename)
        {
            // Dosya içeriğini oku
            byte[] fileData = File.ReadAllBytes(filename);
            if (fileData.Length < 20)
                return;


            listBox1.Items.Clear();
            listBox1.Items.Add(CheckBankType(fileData));


        }

        public static string DecipherROT(byte[] fileBytes)
        {
            byte[] decryptedBytes = new byte[fileBytes.Length]; // Yeni byte dizisi oluştur

            for (int i = 0; i < fileBytes.Length; i++)
            {
                if (fileBytes[i] == 0xAC) decryptedBytes[i] = 32;
                else
                    decryptedBytes[i] = (byte)(fileBytes[i] + 10); // Her byte'ı 10 artır

            }

            return Encoding.ASCII.GetString(decryptedBytes); // Byte dizisini döndür
        }
        public byte[] CipherROT(string input)
        {
            byte[] encryptedBytes = new byte[input.Length]; // Yeni byte dizisi oluştur

            for (int i = 0; i < input.Length; i++)
            {
                encryptedBytes[i] = (byte)(input[i] - 10); // Her karakterin byte değerini 10 azalt
            }

            return encryptedBytes; // Byte dizisini döndür
        }

        public string CheckBankType(byte[] fileData)
        {


            // İlk 4 baytı okuyarak magic değeri elde et
            string magic = Encoding.ASCII.GetString(fileData, 0, 4);
            string bankName = Encoding.ASCII.GetString(fileData, 12, 8).Trim();
            // Eğer bank dosyası T_AMBK ise offset 12’den bank adını al


            // Bank adını kontrol ederek hangi T_AMBK tipi olduğunu belirle
            if (bankName.Equals("Pac.Pic.", StringComparison.OrdinalIgnoreCase))
            {
                parsePic(fileData);
                return "Pac.Pic.";
            }

            else
            {

                string searchString = "Pac.Pic";
                byte[] searchBytes = Encoding.ASCII.GetBytes(searchString);
                int index = IndexOf(fileData, searchBytes);
                if (index >= 0)
                {
                    int offset = index - 12;

                    if (offset < 0)
                    {
                        // Başa (12 - index) kadar 0 ekle
                        byte[] padding = new byte[-offset]; // otomatik olarak 0 ile dolar
                        byte[] newData = new byte[padding.Length + fileData.Length];
                        Buffer.BlockCopy(padding, 0, newData, 0, padding.Length);
                        Buffer.BlockCopy(fileData, 0, newData, padding.Length, fileData.Length);
                        fileData = newData;
                    }
                    else if (offset > 0)
                    {
                        // Baştan offset kadar byte at
                        byte[] newData = new byte[fileData.Length - offset];
                        Buffer.BlockCopy(fileData, offset, newData, 0, newData.Length);
                        fileData = newData;
                    }
                    parsePic(fileData);
                    return "Pac.Pic Forced.";
                }
                else
                {
                    int x = 0; int y = 0; int maxh = 0;
                    byte[] searchBytes2 = { 0x06, 0x07, 0x19, 0x63 };
                    int index2 = IndexOf(fileData, searchBytes2);
                    bool first = true;
                    while (index2 >= 0)
                    {
                        int offset = index2;
                        // Baştan offset kadar byte at
                        byte[] newData = new byte[fileData.Length - offset];
                        Buffer.BlockCopy(fileData, offset, newData, 0, newData.Length);
                        fileData = newData;
                        byte[] prefix = new byte[] {
                            0x00, 0x00, 0x00, 0x0C,
                            0x00, 0x01, 0x00, 0x00,
                            0x50, 0x61, 0x63, 0x2E,
                            0x50, 0x69, 0x63, 0x2E,
                            0x00, 0x00, 0x00, 0x0C
                        };

                        byte[] result = new byte[prefix.Length + fileData.Length];
                        Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
                        Buffer.BlockCopy(fileData, 0, result, prefix.Length, fileData.Length);

                        // artık result, prefix + newData içeriyor
                        fileData = result;

                        parsePic(fileData, x, y, first);
                        if (maxh < PicBank.lumph * PicBank.lumps) maxh = PicBank.lumph * PicBank.lumps;

                        x += PicBank.width * 8;
                        if (x+112 > pictureBox1.Width)
                        {
                            x = 0;
                            y += maxh;
                            maxh = 0;
                        }
                        first = false;
                        index2 = IndexOf(fileData, searchBytes2, 21);
                    }


                }
                return $"Bilinmeyen bank tipi (magic: '{magic}')";
            }
        }

        void parsePic(byte[] fileData, int x = 0, int y = 0, bool clear = true)
        {
            PicBank = MakePalette(fileData);
            listBox1.Items.Add((PicBank.width * 8).ToString() + "x" + (PicBank.lumph * PicBank.lumps).ToString());
            PicBank = Decompress(PicBank);
            PicBank = PaintAmigaPic(PicBank, x, y, clear);

        }

        // Byte array içinde başka bir byte array arayan metod
        public static int IndexOf(byte[] haystack, byte[] needle, int start = 0)
        {
            for (int i = start; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return i;
            }
            return -1;
        }

        public AmigaPic MakePalette(byte[] fileData)
        {
            AmigaPic pic = new AmigaPic();
            // 32 renklik palet için dizi oluştur
            pic.palette = new Color[32];

            // Başlangıç offset'i
            int o = 20;

            // Dosyanın boyutunun yeterli olup olmadığını kontrol et
            if (fileData.Length < o + 26 + 32 * 2)
                listBox1.Items.Add("Dosya boyutu yetersiz.");

            // HAM başlık kontrolü: offset 20'den 4 bayt okunuyor (big-endian)
            uint magic = (uint)((fileData[o] << 24) | (fileData[o + 1] << 16) | (fileData[o + 2] << 8) | fileData[o + 3]);
            if (magic == 0x12031990)
            {
                // Paletin bulunduğu offset: o+26 dan itibaren 32 renk, her renk 2 bayt
                int palStart = o + 26;
                for (int i = 0; i < 32; i++)
                {
                    int index = palStart + i * 2;
                    if (index + 1 < fileData.Length)
                    {
                        // 2 baytı big-endian olarak birleştiriyoruz
                        ushort word = (ushort)((fileData[index] << 8) | fileData[index + 1]);
                        // Amiga renk formatı 12-bit (4 bit her kanal) olarak saklanır.
                        // 0-15 arası değeri 0-255 aralığına ölçeklendiriyoruz (17 ile çarparak)
                        int r = ((word >> 8) & 0x0F) * 17;
                        int g = ((word >> 4) & 0x0F) * 17;
                        int b = (word & 0x0F) * 17;
                        pic.palette[i] = Color.FromArgb(r, g, b);
                        if (!checkBox4.Checked) BackupPalette[i] = Color.FromArgb(r, g, b);
                    }
                    else
                    {
                        listBox1.Items.Add("Palet verileri eksik.");
                    }
                }
                // Palet okunduktan sonra, offset'i resim başlığına ilerletmek için güncelliyoruz
                o += 90;
            }
            else
            {
                for (int i = 0; i < 32; i++)
                {
                    pic.palette[i] = Color.FromArgb(i * 7, i * 7, i * 7);
                }
                listBox1.Items.Add("HAM başlığı bulunamadı, dosya formatı beklenenden farklı.");
            }
            if (checkBox4.Checked)
            {
                for (int i = 0; i < 32; i++)
                {
                    pic.palette[i] = BackupPalette[i];
                }
            }
            // Resim başlığının kontrolü: Beklenen magic değeri 0x06071963
            if (fileData.Length < o + 24)
                listBox1.Items.Add("Dosya boyutu yetersiz, resim başlığı eksik.");

            uint picHeader = (uint)((fileData[o] << 24) | (fileData[o + 1] << 16) | (fileData[o + 2] << 8) | fileData[o + 3]);
            if (picHeader != 0x06071963)
            {
                listBox1.Items.Add("Resim başlığı bulunamadı.");
            }
            // Genişlik ve yüksekliği oku (big-endian: 2 bayt)
            pic.width = ((fileData[o + 8] << 8) | fileData[o + 9]); //width as bytes
            pic.lumph = (fileData[o + 10] << 8) | fileData[o + 11]; //lump count

            // Lumps (ll) ve Bitplane Sayısı (dlumps) Okuma
            pic.lumps = (fileData[o + 12] << 8) | fileData[o + 13]; // ll (linelumps height)
            pic.dlumps = (fileData[o + 14] << 8) | fileData[o + 15]; // d (bitplane sayısı)

            pic.rleDataOffset = get4(fileData, o + 16) - 24;  //-24 due to removing the header from the stream
            pic.pointsOffset = get4(fileData, o + 20) - 24;



            // Piksel verisinin başladığı offset: o+24'ten itibaren dosyanın sonuna kadar
            int pixelDataOffset = o + 24;
            int pixelDataLength = fileData.Length - pixelDataOffset;
            pic.pixelData = new byte[pixelDataLength];
            Array.Copy(fileData, pixelDataOffset, pic.pixelData, 0, pixelDataLength);
            listBox1.Items.Add("Render tamam.");
            return pic;
        }

        int get4(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) |
                   (data[offset + 2] << 8) | data[offset + 3];
        }

        public AmigaPic Decompress(AmigaPic pic)
        {
            // Yerel değişkenlere kısaltmalar:
            int w = pic.width;      // Dosyada saklanan genişlik (byte cinsinden)
            int lh = pic.lumph;     // Yükseklik (satır sayısı)
            int ll = pic.lumps;     // Linelumps sayısı
            int d = pic.dlumps;     // Bitplane (dlumps) sayısı

            // Çözülen (uncompressed) veri boyutu:
            int totalSize = w * lh * ll * d;
            byte[] uncompressed = new byte[totalSize];

            // pic.pixelData içerisinde;
            // - ilk bölüm: "picdata" (sıkıştırılmış piksel verisi)
            // - rleDataOffset: rledata'nın başladığı index (pixelData içerisindeki)
            // - pointsOffset: points verisinin başladığı index
            int picdataPos = 0;                     // picdata'nın başlangıcı (pixelData[0])
            int rledataPos = pic.rleDataOffset;       // RLE kontrol baytlarının başladığı yer
            int pointsPos = pic.pointsOffset;         // Ek kontrol (points) baytlarının başladığı yer

            // RLE bit sayaçları
            int rbit = 7;
            int rrbit = 6;

            // İlk piksel baytını ve RLE baytını oku
            int picbyte = pic.pixelData[picdataPos++];
            int rlebyte = pic.pixelData[rledataPos++];

            // Eğer points'in en yüksek biti set ise, ek bir RLE baytı oku
            if ((pic.pixelData[pointsPos] & 0x80) != 0)
            {
                rlebyte = pic.pixelData[rledataPos++];
            }

            // Döngü yapısı orijinal C koduyla aynı mantıkta:
            // d: bitplane sayısı, lh: lump sayısı, w: sütun sayısı, ll: lump line height (linelumps) sayısı
            for (int i = 0; i < d; i++)
            {
                // Her bitplane için, uncompressed verideki başlangıç index'i:
                int planeOffset = i * (w * lh * ll);
                //unsigned char *lump_start = bitplane_ptrs[i];


                for (int j = 0; j < lh; j++)
                {
                    // j. satırın lump bölümünün başlangıcı:
                    int rowOffset = planeOffset + j * (w * ll);
                    //unsigned char *lump_offset = lump_start;

                    for (int k = 0; k < w; k++)
                    {
                        // k. sütunun lump içindeki başlangıcı:
                        int baseIndex = rowOffset + k;
                        //unsigned char *d = lump_offset;

                        for (int l = 0; l < ll; l++)
                        {
                            // Eğer mevcut RLE baytındaki (rlebyte) rbit konumundaki bit 1 ise,
                            // yeni bir piksel baytı oku.
                            if ((rlebyte & (1 << rbit)) != 0)
                            {
                                picbyte = pic.pixelData[picdataPos++];
                            }
                            rbit--;  // Sonrasında rbit'i bir azalt

                            // Hesaplanan çıkış indeksi:
                            // Her lump (line) için çıktı, aynı sütunda fakat w kadar aralıkla yer alıyor.
                            int outIndex = baseIndex + l * w;
                            uncompressed[outIndex] = (byte)picbyte;

                            // Eğer rbit sıfırın altına düştüyse, kontrol için:
                            if (rbit < 0)
                            {
                                rbit = 7; // rbit'i resetle

                                // POINTS baytındaki rrbit konumundaki bit 1 ise, yeni bir RLE baytı oku.
                                if ((pic.pixelData[pointsPos] & (1 << rrbit)) != 0)
                                {
                                    try
                                    {
                                        rlebyte = pic.pixelData[rledataPos++];
                                    }
                                    catch
                                    {
                                        pic.uncompressedData = uncompressed;
                                        return pic;
                                    }


                                }
                                rrbit--; // rrbit'i azalt
                                if (rrbit < 0)
                                {
                                    rrbit = 7; // Resetle
                                    pointsPos++; // Bir sonraki points baytına geç
                                }

                            }
                        }
                    }
                }
            }

            // Çözülen veriyi AmigaPic yapısına atayalım:
            pic.uncompressedData = uncompressed;
            return pic;
        }


        public AmigaPic PaintAmigaPic(AmigaPic pic, int destX = 0, int destY = 0, bool clearFirst = true)
        {
            int finalWidth = pic.width * 8;
            int finalHeight = pic.lumph * pic.lumps;
            int w = pic.width;
            int h = pic.lumph;
            int ll = pic.lumps;
            int d = pic.dlumps;

            byte[] chunky = new byte[finalWidth * finalHeight];
            byte[,] chunkyPic = new byte[finalWidth, finalHeight];

            for (int bp = 0; bp < d; bp++)
            {
                int bit = 1 << bp;
                int planeOffset = bp * (w * h * ll);

                for (int y = 0; y < h; y++)
                {
                    for (int l = 0; l < ll; l++)
                    {
                        int row = y * ll + l;
                        for (int x = 0; x < w; x++)
                        {
                            int byteIndex = planeOffset + (row * w) + x;
                            byte v = pic.uncompressedData[byteIndex];
                            for (int j = 0; j < 8; j++)
                            {
                                int col = x * 8 + (7 - j);
                                if ((v & (1 << j)) != 0)
                                {
                                    chunky[row * finalWidth + col] |= (byte)bit;
                                }
                            }
                        }
                    }
                }
            }

            Bitmap targetBmp = null;
            bool newBitmapCreated = false;

            if (clearFirst || pictureBox1.Image == null)
            {
                if (pictureBox1.Image != null && clearFirst)
                {
                    // Dispose previous image only if clearing explicitly
                    pictureBox1.Image.Dispose();
                }
                // Create a new bitmap with PictureBox dimensions if clearing or none exists
                targetBmp = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(targetBmp))
                {
                    // Clear with PictureBox background or a default color (e.g., White)
                    g.Clear(pictureBox1.BackColor);
                }
                pictureBox1.Image = targetBmp;
                newBitmapCreated = true;
            }
            else
            {
                // Attempt to use the existing image
                targetBmp = pictureBox1.Image as Bitmap;
                // If existing image is not a Bitmap or not 24bpp, create a new one and copy old content
                if (targetBmp == null || targetBmp.PixelFormat != PixelFormat.Format24bppRgb)
                {
                    Bitmap oldImage = (Bitmap)pictureBox1.Image;
                    targetBmp = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(targetBmp))
                    {
                        g.Clear(pictureBox1.BackColor); // Start with background
                        if (oldImage != null)
                        {
                            g.DrawImageUnscaled(oldImage, 0, 0); // Copy old image content
                            oldImage.Dispose(); // Dispose the old non-compatible image
                        }
                    }
                    pictureBox1.Image = targetBmp;
                    newBitmapCreated = true; // Technically we created a new one here too
                }
            }

            if (targetBmp == null)
            {
                // Should not happen with the logic above, but as a safeguard
                throw new InvalidOperationException("Failed to get or create a valid Bitmap for the PictureBox.");
            }


            int ncol = 1 << d;
            BitmapData bmpData = targetBmp.LockBits(new Rectangle(0, 0, targetBmp.Width, targetBmp.Height),
                                                    ImageLockMode.ReadWrite, targetBmp.PixelFormat); // Use ReadWrite
            int stride = bmpData.Stride;
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(stride) * targetBmp.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy existing bitmap data into buffer *before* drawing onto it
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int sourceY = 0; sourceY < finalHeight; sourceY++)
            {
                for (int sourceX = 0; sourceX < finalWidth; sourceX++)
                {
                    int targetX = destX + sourceX;
                    int targetY = destY + sourceY;

                    // Store chunky index regardless of whether it's drawn
                    byte index = chunky[sourceY * finalWidth + sourceX];
                    chunkyPic[sourceX, sourceY] = index;

                    // Check if the target pixel is within the bounds of the target bitmap
                    if (targetX >= 0 && targetX < targetBmp.Width && targetY >= 0 && targetY < targetBmp.Height)
                    {
                        if (index >= ncol)
                            index = 0;

                        Color c = pic.palette[index];
                        int pos = targetY * stride + targetX * 3;

                        // Ensure position is valid within the buffer (safety check)
                        if (pos >= 0 && pos + 2 < bytes)
                        {
                            rgbValues[pos] = c.B;
                            rgbValues[pos + 1] = c.G;
                            rgbValues[pos + 2] = c.R;
                        }
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            targetBmp.UnlockBits(bmpData);

            pic.indexedImage = chunkyPic;

            // If we didn't create a new Bitmap instance, we need to refresh the PictureBox
            // to show the changes made to the existing Bitmap object.
            if (!newBitmapCreated)
            {
                pictureBox1.Refresh();
            }
            // If a new bitmap *was* created, assigning it to pictureBox1.Image handles the update.

            return pic;
        }
        public AmigaPic PaintAmigaPic2(AmigaPic pic)
        {
            // Final resim boyutlarını hesaplayalım.
            // pic.width: satırdaki byte sayısı, her byte 8 piksel içerir.
            int finalWidth = pic.width * 8;
            // Final yüksekliği: scanline grubu sayısı * her gruptaki lump sayısı.
            int finalHeight = pic.lumph * pic.lumps;
            int w = pic.width;    // satırdaki byte sayısı
            int h = pic.lumph;   // scanline grubu sayısı
            int ll = pic.lumps;   // her gruptaki lump sayısı
            int d = pic.dlumps;   // bitplane (katman) sayısı

            // chunky formata çevrilecek piksel indeksi dizisini oluşturalım.
            // Her piksel, 0-255 arası bir palet indeksi (gerçekte 1 << d renk kullanılacak) tutacak.
            byte[] chunky = new byte[finalWidth * finalHeight];
            byte[,] chunkyPic = new byte[finalWidth, finalHeight];
            // Bitplaneleri chunky formata dönüştürelim.
            // Her bitplane, uncompressedData dizisinde ardışık blok olarak saklanıyor:
            // Boyutu: w * h * ll (byte) her plane için.
            for (int bp = 0; bp < d; bp++)
            {
                // bp bitplanesi için bit değeri (örneğin, bp = 0 → bit = 1, bp = 1 → bit = 2, vb.)
                int bit = 1 << bp;
                // Bu bitplanesinin verileri, uncompressedData dizisinde şu offset'ten başlar:
                int planeOffset = bp * (w * h * ll);

                // Her bir scanline grubunu (y) ve lump içindeki satır (l) üzerinde ilerleyelim.
                for (int y = 0; y < h; y++)
                {
                    for (int l = 0; l < ll; l++)
                    {
                        // Final resimdeki satır indeksi:
                        int row = y * ll + l;
                        // Satırda w byte bulunuyor (her byte 8 piksel içerir)
                        for (int x = 0; x < w; x++)
                        {
                            // Bu bitplane'daki ilgili byte'yı oku.
                            // Her satırın uzunluğu w, dolayısıyla:
                            int byteIndex = planeOffset + (row * w) + x;
                            byte v = pic.uncompressedData[byteIndex];

                            // Byte'nın her bir bitini kontrol edelim.
                            // Orijinal C kodunda, bitlerin sırası ters çevrilmiş (7-j) olarak hesaplanıyor.
                            for (int j = 0; j < 8; j++)
                            {
                                // Final resimdeki piksel sütun indeksi:
                                int col = x * 8 + (7 - j);
                                // Eğer bu bit set ise, ilgili pikselin chunky değerine o bitplane'nin bit değerini ekle.
                                if ((v & (1 << j)) != 0)
                                {
                                    chunky[row * finalWidth + col] |= (byte)bit;
                                }
                            }
                        }
                    }
                }
            }



            // Şimdi chunky veriden Bitmap oluşturalım.
            // Burada her piksel, chunky dizisinde bir palet indeksi (0 .. (1<<d)-1) olarak yer alıyor.
            // Final renk sayısı:
            int ncol = 1 << d;
            Bitmap bmp = new Bitmap(finalWidth, finalHeight, PixelFormat.Format24bppRgb);

            // Hızlı pixel erişimi için LockBits kullanıyoruz.
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, finalWidth, finalHeight),
                                                ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr ptr = bmpData.Scan0;
            byte[] rgbValues = new byte[stride * finalHeight];

            // chunky dizideki her pikselin palet index'ine göre Bitmap'e renk atayalım.
            for (int y = 0; y < finalHeight; y++)
            {
                for (int x = 0; x < finalWidth; x++)
                {
                    byte index = chunky[y * finalWidth + x];
                    chunkyPic[x, y] = index;
                    // Eğer index, kullanılabilir renk sayısından (ncol) büyükse, varsayılan olarak 0 alalım.
                    if (index >= ncol)
                        index = 0;
                    // AmigaPic.palette dizisinde renkler saklanıyor.
                    Color c = pic.palette[index];
                    int pos = y * stride + x * 3;
                    // 24bpp formatında, renk sırası genellikle BGR şeklindedir.
                    rgbValues[pos] = c.B;
                    rgbValues[pos + 1] = c.G;
                    rgbValues[pos + 2] = c.R;
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);
            bmp.UnlockBits(bmpData);

            pic.indexedImage = chunkyPic;
            // Oluşturulan Bitmap'i PictureBox1'in Image özelliğine atayalım.
            pictureBox1.Image = bmp;
            return pic;

        }

        public LaleMap ListData(AmigaPic pic)
        {
            // ListBox1'in önce temizlendiğinden emin olalım.
            listBox1.Items.Clear();
            LaleMap map = new LaleMap(); // Initialize the map variable
                                         // --- 1. Header Satırı ---
                                         // y = 0 satırının ilk 20 sütun değerini tablarla ayrılmış bir satır olarak ekle.
            string headerLine = "Header:\t"; // Her satırın başına "Idx:" ekle

            for (int x = 0; x < 33; x++)
            {
                headerLine += pic.indexedImage[x, 0].ToString() + "\t";
            }
            listBox1.Items.Add(headerLine);

            // --- 2. Data Length Hesaplama ---
            // indexedImage[4,0] ve [5,0] sırasıyla yatay ve dikey değeri içeriyor.
            int horizontal = pic.indexedImage[4, 0];
            int vertical = pic.indexedImage[5, 0];
            int eventStartIndex = pic.indexedImage[24, 0];
            int dataLength = horizontal * vertical;
            map.width = horizontal;
            map.height = vertical;
            textBox2.Text = eventStartIndex.ToString();


            // --- 3. Data Satırlarını Listeleme ---
            // Data satırları y=1'den itibaren başlıyor.
            int count = 0;         // Eklenen toplam indeks sayısı
            int currentColumn = 0; // Her defasında hangi sütundan başlayacağımız
            int imageWidth = pic.indexedImage.GetLength(0);
            int imageHeight = pic.indexedImage.GetLength(1);


            // dataLength kadar indeks eklenene kadar devam et.
            while (count < dataLength && currentColumn < imageWidth)
            {
                // y=1'den başlayarak tüm satırlar üzerinde dönelim.
                for (int y = 1; y < imageHeight && count < dataLength; y++)
                {
                    // Satır başına 20 sütun alalım: x = currentColumn'dan currentColumn+20'e kadar.
                    int endColumn = Math.Min(currentColumn + 19, imageWidth);
                    string line = "Idx " + (count + 1).ToString() + ":"; // Her satırın başına "Idx:" ekle

                    for (int x = currentColumn; x < endColumn && count < dataLength; x++)
                    {
                        line += "\t" + pic.indexedImage[x, y].ToString();

                    }
                    listBox1.Items.Add(line);
                    count++;

                    // Eğer dataLength kadar indeks eklenmişse, döngüden çık.
                    if (count >= dataLength)
                        break;
                }
                // Eğer tüm satırlar tarandı fakat dataLength'e ulaşamadıysak,
                // sütun başlangıcını 19 artırıp (yeni blok) tekrar y=1'den başlıyoruz.
                currentColumn += 19;
            }

            return map;
        }

        public void SaveListAsTxt(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                foreach (var item in listBox1.Items)
                {
                    string line = item.ToString();

                    // İlk ":" karakterinin indeksini buluyoruz.
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        // ":" karakteri de dahil olmak üzere, baştaki kısmı kaldırıyoruz.
                        line = line.Substring(colonIndex + 1);
                    }

                    // Tüm tab karakterlerini tek boşluk ile değiştiriyoruz.
                    line = line.Replace('\t', ' ');

                    // İstenmeyen boşlukları temizleyelim.
                    line = line.Trim();

                    sw.WriteLine(line);
                }
            }
        }


        private List<string> AnalyzeBank(string filename)
        {
            List<string> results = new List<string>();

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                // 1️⃣ Header'ı oku
                reader.ReadBytes(10); // İlk 10 byte başlığı atlıyoruz
                int dataLength = (reader.ReadByte() << 8) | reader.ReadByte(); // Big-endian uzunluk
                string bankName = Encoding.ASCII.GetString(reader.ReadBytes(8)).Trim();

                results.Add($"Bank Name: {bankName}");
                results.Add($"Data Length: {dataLength} bytes");

                // 2️⃣ Veri okuma ve analiz
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    byte firstByte = reader.ReadByte();

                    if (firstByte == 0xAC) // Metin bitiş baytı
                    {
                        results.Add("AC Termination Found - End of Text Block");
                        continue;
                    }

                    // Muhtemelen metin uzunluğu
                    int textLength = firstByte;
                    if (textLength > 0 && textLength < 255) // Mantıklı bir uzunluk
                    {
                        byte[] encryptedText = reader.ReadBytes(textLength);
                        string decryptedText = DecodeROT10(encryptedText);

                        results.Add($"[Text] {decryptedText}");
                    }

                    // Sonrasında `AC` içermeyen seçenek olup olmadığını kontrol edelim.
                    if (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte nextByte = reader.ReadByte();
                        if (nextByte != 0xAC)
                        {
                            reader.BaseStream.Seek(-1, SeekOrigin.Current); // Okuma konumunu geri al
                            int optionLength = reader.ReadByte();
                            byte[] optionData = reader.ReadBytes(optionLength);
                            string decodedOption = DecodeROT10(optionData);

                            results.Add($"[Option] {decodedOption}");
                        }
                    }
                }
            }

            return results;
        }

        private string DecodeROT10(byte[] data)
        {
            char[] decoded = new char[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                decoded[i] = (char)(data[i] + 10); // ROT10 Çözme
            }
            return new string(decoded);
        }
        public byte[] findNextPic(byte[] fileData, int startindex)
        {
            // 1. Adım: Dosya içinde "Pac.Pic." imzasını ara
            string pacPicSignature = "Pac.Pic.";
            byte[] pacPicBytes = Encoding.ASCII.GetBytes(pacPicSignature);
            int pacPicIndex = -1;
            for (int i = startindex; i <= fileData.Length - pacPicBytes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pacPicBytes.Length; j++)
                {
                    if (fileData[i + j] != pacPicBytes[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    pacPicIndex = i;
                    break;
                }
            }

            if (pacPicIndex == -1)
            {
                // "Pac.Pic." imzası bulunamadı.
                return null;
            }

            // 2. Adım: "Pac.Pic." imzasından itibaren hex dizisi 06 07 19 63'ü ara.
            byte[] headerMarker = new byte[] { 0x06, 0x07, 0x19, 0x63 };
            int headerIndex = -1;
            for (int i = pacPicIndex; i <= fileData.Length - headerMarker.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < headerMarker.Length; j++)
                {
                    if (fileData[i + j] != headerMarker[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex == -1)
            {
                // Picture header marker bulunamadı.
                return null;
            }

            // 3. Adım: Picture header için en az 24 byte veri olduğundan emin olalım.
            if (headerIndex + 24 > fileData.Length)
            {
                // Yeterli veri yok.
                return null;
            }

            // Picture header yapısı (big-endian):
            // Offset 0: 4 byte  sabit header ID, 0x06071963 (doğrulanabilir)
            // Offset 4: 2 byte  ekran içindeki X koordinat offset'i (işlemeye gerek yok)
            // Offset 6: 2 byte  ekran içindeki Y koordinat offset'i (işlemeye gerek yok)
            // Offset 8: 2 byte  resim genişliği (byte cinsinden)
            // Offset 10: 2 byte resim yüksekliği, "line lumps" cinsinden
            // Offset 12: 2 byte her lump'taki satır sayısı
            // Offset 14: 2 byte bitplane sayısı
            // Offset 16: 4 byte RLEDATA stream offset'i (işlemeye gerek yok)
            // Offset 20: 4 byte POINTS stream offset'i (işlemeye gerek yok)

            // Yardımcı fonksiyon: big-endian 16-bit değer oku
            int ReadUInt16(byte[] data, int index)
            {
                return (data[index] << 8) | data[index + 1];
            }

            int widthBytes = ReadUInt16(fileData, headerIndex + 8);
            int heightLineLumps = ReadUInt16(fileData, headerIndex + 10);
            int linesPerLump = ReadUInt16(fileData, headerIndex + 12);
            int bitplanes = ReadUInt16(fileData, headerIndex + 14);

            // Gerçek resim yüksekliğini hesapla (satır sayısı)
            int totalLines = heightLineLumps * linesPerLump;
            // İmaj verisinin uzunluğu: genişlik (byte) * toplam satır sayısı * bitplane sayısı
            int imageDataLength = widthBytes * totalLines * bitplanes;

            // Toplam pac.pic verisi; header (24 byte) + imaj verisi
            int totalPicDataLength = (pacPicIndex - headerIndex) + 24 + imageDataLength;
            // Eğer dosyada bu uzunlukta veri yoksa, kalan veriyi kopyala.
            if (pacPicIndex + totalPicDataLength > fileData.Length)
            {
                totalPicDataLength = fileData.Length - pacPicIndex;
            }

            // 4. Adım: Yeni dizi oluştur (başta 12 byte boş alan olacak)
            int newArrayLength = 12 + totalPicDataLength;
            byte[] filePicture = new byte[newArrayLength];
            // İlk 12 byte zaten 0 değerinde.

            // Header'dan itibaren, hesaplanan uzunluktaki veriyi kopyala.
            Array.Copy(fileData, pacPicIndex, filePicture, 12, totalPicDataLength);
            searchindex = headerIndex + 4;

            return filePicture;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Dialog ayarları
            openFileDialog.Title = "Bir dosya seçin";
            openFileDialog.Filter = "Tüm dosyalar (*.*)|*.*"; // İsteğe göre filtre ekleyebilirsiniz

            // Dialog penceresini göster ve kullanıcı OK'e basarsa
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                // Seçilen dosyayı oku
                string filePath = openFileDialog.FileName;
                byte[] fileData = File.ReadAllBytes(filePath);
                searchindex = 0;
                byte[] picData = findNextPic(fileData, searchindex);
                if (picData == null)
                {
                    return;
                }
                listBox1.Items.Clear();
                listBox1.Items.Add(CheckBankType(picData));
                listBox1.Items.Add(searchindex.ToString());

            }



        }

        private void button5_Click(object sender, EventArgs e)
        {
            SaveListAsTxt(textBox1.Text + "_LMap" + ".txt");
            MessageBox.Show(textBox1.Text + "_LMap" + ".txt is saved.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Dialog ayarları
            openFileDialog.Title = "Bir dosya seçin";
            openFileDialog.Filter = "Tüm dosyalar (*.*)|*.*"; // İsteğe göre filtre ekleyebilirsiniz

            // Dialog penceresini göster ve kullanıcı OK'e basarsa
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                // Seçilen dosyayı oku
                string filePath = openFileDialog.FileName;
                byte[] fileData = File.ReadAllBytes(filePath);

                byte[] decryptedBytes = new byte[fileData.Length]; // Yeni byte dizisi oluştur

                for (int i = 0; i < fileData.Length; i++)
                {
                    decryptedBytes[i] = fileData[i];
                    if ((i > 0x027e) && (i < 0x045f))
                    {
                        //40-7b
                        byte decoded = (byte)(fileData[i] + 10);
                        if ((decoded > 0x20 - 10) && (decoded < 0x7F))
                        {
                            decryptedBytes[i] = (byte)(fileData[i] + 10); // Her byte'ı 10 artır
                        }
                    }
                }

                File.WriteAllBytes(filePath + "outy.dat", decryptedBytes);

            }
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = listBox2.SelectedIndex;
            if (index < 0 || index > eventDataList.Count) return;

            byte[] data = eventDataList[index];
            textBox2.Text = ViewEvent(data);
            if (checkBox1.Checked) textBox2.Text += ViewEvent2(data);
            if (checkBox3.Checked)
            {
                if (listBox2.SelectedIndex >= 0)
                    drawDungeon(1 + listBox2.SelectedIndex + eventOffset);
                else
                    drawDungeon();
            }
        }



        private string ViewEvent(byte[] eventData)
        {
            StringBuilder sb = new StringBuilder();
            int offset = 0;
            int linecounter = 0;
            // İlk 8 baytı atla
            if (eventData.Length < 9) return "[Geçersiz event]";

            // İlk 8 byte'ı göster
            if (checkBox2.Checked)
            {
                sb.Append("Header: ");
                for (int i = 0; i < 8; i++)
                {
                    sb.Append($"{eventData[i]:X2} ");
                }
                sb.AppendLine();
                sb.AppendLine();
            }
            offset = 8;


            while (offset < eventData.Length)
            {
                byte cmd = eventData[offset++];
                bool match = false;
                int ofs = offset;
                switch (cmd)
                {
                    case 0x0D: //oda başlangıcı (1 parametre)
                        byte blokimg = eventData[offset];
                        offset++;
                        sb.AppendLine(linecounter++.ToString() + "." + "Room Begin: (" + blokimg.ToString() + ")");
                        break;

                    case 0x01: //IF
                        match = true;
                        if (offset + 3 <= eventData.Length)
                        {
                            byte var = eventData[offset++];
                            byte equals = eventData[offset++];
                            byte jumpOver = eventData[offset++];
                            //byte jumpElse = eventData[offset++];
                            if (var == 1) sb.AppendLine(linecounter++.ToString() + "." + $"IF isNotSet({equals}) Then SKIP {jumpOver} ({(jumpOver + linecounter)})");//,  else={jumpElse}]");
                            else if (var == 2) sb.AppendLine(linecounter++.ToString() + "." + $"IF AnswerWas({equals}) Then SKIP {jumpOver} ({(jumpOver + linecounter)})");
                            else sb.AppendLine(linecounter++.ToString() + "." + $"IF Operator({var})->({equals}) Then SKIP {jumpOver} ({(jumpOver + linecounter)})");
                        }
                        break;

                    case 0x02: //sadece yazı göster
                        match = true;
                        if (offset < eventData.Length)
                        {
                            byte len = eventData[offset++];
                            if (offset + len <= eventData.Length)
                            {
                                byte[] txt = new byte[len];
                                Array.Copy(eventData, offset, txt, 0, len);
                                string text = DecipherROT(txt).Replace("\xAC", " ");
                                sb.AppendLine(linecounter++.ToString() + "." + $"Print \"{text}\"");
                                offset += len;
                            }
                        }
                        break;

                    case 0x03: //overlay icon göster
                        match = true;
                        if (offset < eventData.Length)
                        {
                            byte imageId = eventData[offset++];
                            sb.AppendLine(linecounter++.ToString() + "." + $"Show Image({imageId})");
                        }
                        break;

                    case 0x04: //odaya kabul et ve blok başlat
                        match = true;
                        if (offset + 2 <= eventData.Length)
                        {
                            byte len2 = eventData[offset + 1];
                            byte len1 = eventData[offset];

                            if (len1 == 0x0E || len1 == 0x00)
                            {

                                offset++;
                                offset++;
                                sb.AppendLine("");
                                sb.AppendLine(linecounter++.ToString() + "." + $"Admit and Begin (0x{len1:X2}) ({len2})");
                            }
                            else if (len1 == 0x0F)
                            {
                                offset++;
                                offset++;
                                if (len2 == 0x00)
                                {
                                    sb.AppendLine("");
                                    sb.AppendLine(linecounter++.ToString() + "." + $"End.");
                                }
                                else
                                {
                                    sb.AppendLine(linecounter++.ToString() + $".Begin Special {len2}");
                                }
                            }
                            else
                            {
                                match = false;
                                sb.AppendLine("");
                                sb.AppendLine(linecounter++.ToString() + "." + $"Begin");
                            }
                        }
                        else if (offset + 1 <= eventData.Length)
                        {
                            byte len1 = eventData[offset];
                            if (len1 == 0x00)
                            {
                                offset++;
                                sb.AppendLine("");
                                sb.AppendLine(linecounter++.ToString() + "." + $"Room End (4-0)");
                            }
                        }

                        break;

                    case 0x05: //seçim yap (0xAC ile ayrılmış 2 parametre)
                        match = true;
                        if (offset + 2 <= eventData.Length)
                        {
                            byte len1 = eventData[offset++];
                            byte len2 = eventData[offset++];
                            if (offset + len1 + len2 <= eventData.Length)
                            {
                                byte[] opt1 = new byte[len1];
                                byte[] opt2 = new byte[len2];
                                Array.Copy(eventData, offset, opt1, 0, len1);
                                offset += len1;
                                Array.Copy(eventData, offset, opt2, 0, len2);
                                offset += len2;

                                string q1 = DecipherROT(opt1).Replace("\xAC", "\n");
                                string q2 = DecipherROT(opt2).Replace("\xAC", "\n");
                                sb.AppendLine(linecounter++.ToString() + "." + $"Present Choice:  {q1} / {q2}");
                            }
                        }
                        break;

                    case 0x06: //bilinmeyen 6 parametreli komut (tahminen 3 adet 2 byte parametre)
                        //06 05 07 81 03 17
                        byte bilinmeyen62 = eventData[offset++];
                        byte bilinmeyen63 = eventData[offset++];
                        byte bilinmeyen64 = eventData[offset++];
                        byte bilinmeyen65 = eventData[offset++];
                        //byte bilinmeyen66 = eventData[offset++];
                        sb.AppendLine(linecounter++.ToString() + "." + $"[UNK4-{cmd:X2}] {bilinmeyen62}, {bilinmeyen63}, {bilinmeyen64}, {bilinmeyen65}]");

                        break;

                    case 0x07: //Değişkeni set et (0-255 değişken numarası)
                        match = true;
                        if (offset + 1 <= eventData.Length)
                        {
                            byte var = eventData[offset++];
                            sb.AppendLine(linecounter++.ToString() + "." + $"SetVariable ({var})."); //, value ={ val}
                        }
                        break;

                    case 0x08: //savaş ekranını başlatır, 7 parametre
                        match = true;
                        if (offset + 7 <= eventData.Length)
                        {
                            byte zero = eventData[offset++];
                            byte enemyId = eventData[offset++];
                            byte count = eventData[offset++];
                            byte bilinmeyen2 = eventData[offset++];
                            byte bilinmeyen3 = eventData[offset++];
                            byte bilinmeyen4 = eventData[offset++];
                            byte bilinmeyen5 = eventData[offset++];
                            if ((bilinmeyen4 < enemies.Length) && (bilinmeyen2 < enemies.Length) && (enemyId < enemies.Length))
                            {
                                sb.AppendLine(linecounter++.ToString() + "." + $" ⚔️⚔️ Fight ⚔️⚔️ {zero} EnemyType1: [{enemyId}] {enemies[enemyId]} X {count}, EnemyType2: [{bilinmeyen2:x2}] {enemies[bilinmeyen2]} X {bilinmeyen3}, EnemyType3: [{bilinmeyen4:x2}] {enemies[bilinmeyen4]} X {bilinmeyen5}");
                            }
                            else
                            {
                                sb.AppendLine(linecounter++.ToString() + "." + $" ⚔️⚔️ Fight ⚔️⚔️ {zero} EnemyType1: [{enemyId}]  X {count}, EnemyType2: [{bilinmeyen2:x2}] X {bilinmeyen3}, EnemyType3: [{bilinmeyen4:x2}] X {bilinmeyen5}");
                            }
                        }
                        break;
                    case 0x09: // loot ver 9 parametre
                        match = true;
                        if (offset + 9 < eventData.Length) // toplam 10 byte
                        {
                            byte dolar = eventData[offset++];
                            byte mark = eventData[offset++];
                            byte kıvrık = eventData[offset++];
                            byte bilinmeyen1 = eventData[offset++];
                            byte itemNo = eventData[offset++];
                            byte bilinmeyen2 = eventData[offset++];
                            byte bilinmeyen3 = eventData[offset++];
                            byte bilinmeyen4 = eventData[offset++];
                            byte bilinmeyen5 = eventData[offset++];

                            sb.AppendLine(linecounter++.ToString() + "." + $"Loot $: {dolar}" + $", M: {mark}" + $", K: {kıvrık}");
                            if (itemNo > 0 && itemNo < 27) sb.AppendLine($"  Loot {bilinmeyen1}  item #{itemNo}({items[itemNo]})"); else sb.AppendLine($"  ?: {bilinmeyen1}  item#: {itemNo}");
                            sb.AppendLine($"  Loot Params: {bilinmeyen2}, {bilinmeyen3}, {bilinmeyen4}, {bilinmeyen5}");
                        }
                        else
                        {
                            sb.AppendLine("[itemAction] -> yetersiz veri");
                            offset = eventData.Length; // veriyi aşmasın
                        }
                        break;

                    case 0x0A: //rastgele iki metinden birini bas
                        match = true;
                        sb.AppendLine(linecounter++.ToString() + "." + "[printRandom]");

                        // İlk metni oku
                        if (offset < eventData.Length)
                        {
                            byte len1 = eventData[offset++];
                            if (offset + len1 <= eventData.Length)
                            {
                                byte[] txt1 = new byte[len1];
                                Array.Copy(eventData, offset, txt1, 0, len1 - 1);
                                offset += len1 - 1;

                                string text1 = DecipherROT(txt1).Replace("\xAC", " ");
                                sb.AppendLine($"- \"{text1}\"");
                            }
                        }

                        // 0xAC kontrolü
                        if (offset < eventData.Length && eventData[offset] == 0xAC)
                        {
                            offset++; // AC'yi geç

                            // İkinci metni oku
                            if (offset < eventData.Length)
                            {
                                byte len2 = eventData[offset++];
                                if (offset + len2 <= eventData.Length)
                                {
                                    byte[] txt2 = new byte[len2];
                                    Array.Copy(eventData, offset, txt2, 0, len2);
                                    offset += len2;

                                    string text2 = DecipherROT(txt2).Replace("\xAC", " ");
                                    sb.AppendLine($"- \"{text2}\"");
                                }
                            }


                            if (offset < eventData.Length && eventData[offset] == 0xAC)
                            {
                                offset++;
                            }
                        }
                        break;

                    case 0x0B: //exit map 0x0B [Harita No] [n1*256+n2 lokasyon] [bakış yönü]
                        match = true;
                        if (offset + 4 < eventData.Length) // toplam 5 byte
                        {
                            byte bilinmeyen2 = eventData[offset++];
                            byte bilinmeyen3 = eventData[offset++];
                            byte bilinmeyen4 = eventData[offset++];
                            byte bilinmeyen5 = eventData[offset++];
                            string[] yon = { "Kuzey ⬆️", "Doğu ➡️", "Güney ⬇️", "Batı ⬅️" };
                            sb.AppendLine(linecounter++.ToString() + "." + $" [*★★**EXIT**★★*] Next Map: {bilinmeyen2}, {(bilinmeyen3 * 256) + bilinmeyen4}, {yon[bilinmeyen5]}");
                        }
                        break;

                    case 0x0E: //Blok sonu
                        match = true;
                        if (offset < eventData.Length)
                        {
                            byte dolar = eventData[offset++];
                            sb.AppendLine(linecounter++.ToString() + "." + $"End {dolar}.");
                        }
                        break;

                    case 0x10:  //exit event.

                        sb.AppendLine(linecounter++.ToString() + "." + "[break]");
                        break;

                    case 0x11: //Konusan kafa ile konusma (0=random, 1-2-3-4 grup, 5+ npc)
                        match = true;
                        if (offset + 1 < eventData.Length)
                        {
                            byte faceId = eventData[offset++];
                            byte len = eventData[offset++];
                            if (offset + len <= eventData.Length)
                            {
                                byte[] txt = new byte[len];
                                Array.Copy(eventData, offset, txt, 0, len);
                                offset += len;
                                string text = DecipherROT(txt).Replace("\xAC", "\n");
                                if (faceId == 0) sb.AppendLine(linecounter++.ToString() + "." + $"Talk Random Face \"{text}\"");
                                else if (faceId > 4) sb.AppendLine(linecounter++.ToString() + "." + $"Talk as NPC {faceId},\"{text}\"");
                                else sb.AppendLine(linecounter++.ToString() + "." + $"Talk with Face {faceId},\"{text}\"");
                            }
                        }
                        break;


                    case 0x12:  //bilinmeyen parametresiz komut
                        match = false;
                        sb.AppendLine(linecounter++.ToString() + "." + $"UNK0[{cmd:X2}] Test?");
                        break;

                    case 0x17: //gösterilecek yüzü ayarla
                        match = true;
                        byte var1 = eventData[offset++];
                        byte var2 = eventData[offset++];
                        sb.AppendLine(linecounter++.ToString() + "." + $"Set Face [17] {var1},\"{var2}\"");

                        break;

                    case 0x0C:  //bilinmeyen 3 parametreli komutlar
                    case 0x16:
                    case 0x1D:
                    case 0x13:
                        match = true;
                        byte var4 = eventData[offset++];
                        byte var5 = eventData[offset++];
                        byte var6 = eventData[offset++];
                        sb.AppendLine(linecounter++.ToString() + "." + $"UNK3 [{cmd:X2}] {var4:X2}, {var5:X2}, {var6:X2}.");

                        break;

                    case 0x19: //bilinmeyen 1 parametreli komut
                    case 0x1A:
                        match = true;
                        byte var1A = eventData[offset++];
                        if (var1A == 255)
                        {
                            sb.AppendLine(linecounter++.ToString() + "." + $"[ Envanterdeki tüm paraları sıfırla ].");
                        }
                        else
                            sb.AppendLine(linecounter++.ToString() + "." + $"[UNK1 {cmd:X2}] {var1A}.");

                        break;

                    case 0x1B: //oyunun hikayesini anlatan cutscene ve copyprotection
                        match = true;
                        sb.AppendLine(linecounter++.ToString() + "." + "[Show Demo And Check WordList]");
                        int wc = 0;
                        while (offset < eventData.Length)
                        {
                            byte len = eventData[offset++];

                            if (len == 0x03 || len == 0x02)
                            {
                                sb.AppendLine("[end of word list]");
                                offset--;
                                break;
                            }

                            if (len >= 0x1F || offset + len > eventData.Length)
                            {
                                sb.AppendLine($"[invalid word entry: len={len}]");
                                break;
                            }

                            // metni oku, ROT10 çöz
                            byte[] raw = new byte[len];
                            Array.Copy(eventData, offset, raw, 0, len);
                            offset += len;

                            int textEnd = 0;
                            for (; textEnd < raw.Length; textEnd++)
                            {
                                if (raw[textEnd] < 0x0F)
                                    break;
                            }

                            byte[] encrypted = raw.Take(textEnd).ToArray();
                            string word = DecipherROT(encrypted); // ROT10 çözümü
                            byte control = (textEnd < raw.Length) ? raw[textEnd] : (byte)0x00;

                            string page = word.Length >= 2 ? word.Substring(0, 2) : "??";
                            string wordNo = word.Length >= 4 ? word.Substring(2, 2) : "??";
                            string realWord = word.Length > 4 ? word.Substring(4) : "";

                            sb.AppendLine($"{wc} - [{page}-{wordNo}]: \"{realWord}\"");
                            wc++;
                            if (textEnd < raw.Length && control == 0x03)
                            {
                                sb.AppendLine("[end of word list]");
                                break;
                            }
                        }
                        break;
                    case 0x20: //bir adım geri git (odaya girilmişse geri çıkarır)
                        match = false;
                        sb.AppendLine(linecounter++.ToString() + "." + $"Retrace (Go Back [14]).");
                        break;


                    default:

                        // Bilinmeyen komut ya da düz veri — ASCII kontrolü
                        char? maybeChar = null;
                        int adjusted = cmd + 10;

                        if (adjusted >= 0x20 && adjusted <= 0x7E)
                            maybeChar = (char)adjusted;

                        if (maybeChar.HasValue)
                            sb.Append($"[{cmd:X2} ({maybeChar})] ");
                        else
                            sb.Append($"[{cmd:X2}] ");

                        break;
                }
                if (match && (ofs == offset))
                {
                    sb.Append($"[0x{cmd:X2}] ");
                }
            }

            return sb.ToString();
        }


        private string ViewEvent2(byte[] eventData)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in eventData)
            {
                // Yazdırılabilir ASCII karakterleri (space dahil, del hariç)
                if (b == 0x0D) sb.Append(" | ");
                //    sb.Append((char)b);
                //else
                sb.Append($" {b:X2}");
            }

            return sb.ToString();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            DumpEventDataList(eventDataList, textBox1.Text + "_events.txt");

        }

        private void DumpEventDataList(List<byte[]> eventDataList, string filename)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    for (int i = 0; i < eventDataList.Count; i++)
                    {
                        sw.WriteLine($"===== Event #{i + 1} =====");
                        string content = ViewEvent(eventDataList[i]);
                        sw.WriteLine(content);
                        sw.WriteLine(); // Araya boşluk
                    }

                    MessageBox.Show(filename + " Dump tamamlandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int move = 250;
            if (button8.Text == "<")
            {
                move = -250;
                button8.Text = ">";
            }
            else
                button8.Text = "<";
            pictureBox1.Width += move;
            listBox2.Left += move;
            listBox1.Left += move;
            listBox1.Width -= move;
            textBox2.Left += move;
            textBox2.Width -= move;
            button8.Left += move;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            groupBox1.Visible = !groupBox1.Visible;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {

            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("Kaydedilecek bir resim yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PNG Dosyası|*.png|JPEG Dosyası|*.jpg|Bitmap Dosyası|*.bmp";
                saveFileDialog.Title = "Resmi Kaydet";
                saveFileDialog.FileName = lastfile + ".png";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;

                    // Dosya uzantısına göre format belirle
                    string ext = Path.GetExtension(saveFileDialog.FileName).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg")
                        format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    else if (ext == ".bmp")
                        format = System.Drawing.Imaging.ImageFormat.Bmp;

                    pictureBox1.Image.Save(saveFileDialog.FileName, format);
                }
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Form paletteForm = new Form();
            paletteForm.Text = "Palette Editor";
            paletteForm.Size = new Size(420, 300);
            paletteForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            paletteForm.MaximizeBox = false;

            for (int i = 0; i < 32; i++)
            {
                Button colorButton = new Button();
                colorButton.Size = new Size(40, 40);
                colorButton.Left = 10 + (i % 8) * 50;
                colorButton.Top = 10 + (i / 8) * 50;
                colorButton.BackColor = BackupPalette[i];
                int index = i; // gerekli, closure hatasını önler

                colorButton.Click += (s, ev) =>
                {
                    using (ColorDialog dlg = new ColorDialog())
                    {
                        dlg.Color = BackupPalette[index];
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            BackupPalette[index] = dlg.Color;
                            colorButton.BackColor = dlg.Color;
                            LoadAbkPic(lastfilepath);
                        }
                    }
                };

                paletteForm.Controls.Add(colorButton);
            }

            paletteForm.ShowDialog();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Height = this.ClientSize.Height - pictureBox1.Top;
        }

        private int lastfoundindex = 0;

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                string selectedText = textBox2.SelectedText;
                if (string.IsNullOrEmpty(selectedText)) return;

                int startIndex = textBox2.SelectionStart + textBox2.SelectionLength;
                int foundIndex = textBox2.Text.IndexOf(selectedText, startIndex, StringComparison.CurrentCultureIgnoreCase);

                if (foundIndex == -1 && startIndex > 0) // başa dönerek ara
                {
                    foundIndex = textBox2.Text.IndexOf(selectedText, 0, StringComparison.CurrentCultureIgnoreCase);
                }

                if (foundIndex != -1)
                {
                    textBox2.SelectionStart = foundIndex;
                    textBox2.SelectionLength = selectedText.Length;
                    textBox2.ScrollToCaret();
                }

                e.Handled = true;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            groupBox2.Visible = !groupBox2.Visible;

        }

        private void button14_Click(object sender, EventArgs e)
        {
            textBox2.Text = Encoding.ASCII.GetString(CipherROT(textBox2.Text));
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            var result = GetRoomAndWallFromPixel(e.X, e.Y);
            if (result.index > 0 && result.index < listBox1.Items.Count)
            {
                textBox2.Text = ($"Tıklanan oda numarası: {result.index}");
                listBox1.SelectedIndex = result.index;
                //0x0B [Harita No] [n1*256+n2 lokasyon] [bakış yönü]
                string input = textBox1.Text;
                string c = "";
                if (byte.TryParse(input, out byte value))
                {
                    //{ (bilinmeyen3 * 256) + bilinmeyen4}
                    int b2 = (int)(result.index / 256);
                    int b1 = result.index - b2;
                    c = "Entry point winuae patch: W 0000ADAB " + value.ToString("X2") + b2.ToString("X2") + b1.ToString("X2") + "04";
                    textBox2.Text += "\r\n" + c;

                }
                textBox2.Text += "\r\n" + describeRoom(listBox1.Items[listBox1.SelectedIndex].ToString());




                ClickedRoomInfo opposite = findOppositeWall(result);

                // veya bir düzenleme penceresi açabilirsiniz
            }
        }


        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!checkBox5.Checked) return;
            var result = GetRoomAndWallFromPixel(e.X, e.Y);
            if (result.index > 0)
            {
                updateRoomHighlight(result.index, result.wall);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                laleExe = File.ReadAllBytes(@"execs\Lale");
                // Önce listeyi al
                var weaponList = ParseWeaponList(0x03ca1c);// 0x3eb38);

                // Sonra ListBox'ı temizleyip yükle
                listBox1.Items.Clear();
                listBox1.Items.AddRange(weaponList.Cast<object>().ToArray());

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosya yüklenirken hata oluştu: {ex.Message}");
            }
        }



        private void parseAll()
        {
            List<StringInfo> allStrings = new List<StringInfo>();
            int currentOffset = 0x037180; // String verilerinin başladığı genel alan

            while (currentOffset < laleExe.Length)
            {
                var result = ParseAmosStyleList(currentOffset);

                if (result.ParsedList.Count > 0)
                {
                    allStrings.AddRange(result.ParsedList);
                    currentOffset = result.NextOffset;
                }
                else
                {
                    break;
                }

                // Basit sonsuz döngü kontrolü
                if (result.NextOffset <= currentOffset && result.ParsedList.Count > 0)
                {
                    // Offset ilerlemiyorsa bir sorun var demektir.
                    // Güvenlik için döngüden çıkalım.
                    // İsteğe bağlı: Hata mesajı gösterilebilir.
                    break;
                }
                // Eğer result.ParsedList boşsa zaten yukarıdaki else bloğu çalışır.
                // Bu kontrol sadece offset'in ilerlediğinden emin olmak için.
                // Daha sağlam bir kontrol: currentOffset'in result.NextOffset'e eşit olup olmadığını kontrol et.
                // Eğer eşitse ve liste boş değilse, ilerleme yok demektir.
                if (currentOffset == result.NextOffset && result.ParsedList.Count > 0)
                {
                    // Offset ilerlemedi, çık.
                    break;
                }


            }

            listBox2.Items.Clear();
            if (allStrings.Count > 0)
            {
                listBox2.Items.AddRange(allStrings.Cast<object>().ToArray());
            }
            else
            {
                listBox2.Items.Add("Hiçbir string listesi bulunamadı veya okunamadı.");
            }
        }





        public struct StringInfo
        {
            public string Text;
            public int Offset;

            public StringInfo(string text, int offset)
            {
                Text = text;
                Offset = offset;
            }

            public override string ToString()
            {
                return $"[{Offset:X6}] {Text}";
            }
        }


        // gemini üretti bunu:
        private List<StringInfo> ParseWeaponList(int startOffset)
        {
            List<StringInfo> list = new List<StringInfo>();
            int currentPos = startOffset;

            Encoding encoding = Encoding.ASCII;

            while (currentPos < laleExe.Length)
            {
                int stringStartOffset = currentPos; // String'in başladığı yer

                // 1. Null karakterini (0x00) bulana kadar ilerle
                int stringEndPos = -1;
                for (int i = currentPos; i < laleExe.Length; i++)
                {
                    if (laleExe[i] == 0x00)
                    {
                        stringEndPos = i; // Null karakterinin pozisyonu
                        break;
                    }
                }

                // Eğer dosya sonuna kadar null bulunamadıysa, liste bitmiş veya veri bozuktur.
                if (stringEndPos == -1)
                {
                    break; // Döngüden çık
                }

                // 2. String'i null karakterine kadar olan kısımdan al
                int length = stringEndPos - currentPos;
                string text = "";
                if (length > 0)
                {
                    text = encoding.GetString(laleExe, currentPos, length);
                }
                // else: Boş string (sadece null karakter vardı), yine de eklenebilir istersen.
                // Şimdilik sadece uzunluğu 0'dan büyükse ekleyelim.

                // String'i ve başlangıç offset'ini listeye ekle
                list.Add(new StringInfo(text, stringStartOffset));

                // 3. Pozisyonu null karakterinden sonraya taşı
                currentPos = stringEndPos + 1;

                // Dosya sonuna geldik mi kontrol et
                if (currentPos >= laleExe.Length) break;

                // 4. Word Alignment için Padding kontrolü:
                // Null'dan sonraki pozisyon (currentPos) çift ise, padding vardır.
                // (Çünkü null tek adresteydi: stringEndPos tekti)
                bool hasPadding = (currentPos % 2 == 0);

                if (hasPadding)
                {
                    // Padding byte'ını atla (genellikle 0x00'dır)
                    // Dosya sonu kontrolü yapalım
                    if (currentPos < laleExe.Length)
                    {
                        // İsteğe bağlı: atlanan byte'ın 0 olup olmadığını kontrol edebilirsin
                        // if(laleExe[currentPos] != 0x00) { Console.WriteLine($"Uyarı: Beklenmeyen padding byte: {laleExe[currentPos]} at {currentPos:X}"); }
                        currentPos++;
                    }
                    else
                    {
                        // Padding bekleniyordu ama dosya bitti.
                        break;
                    }
                }

                // Dosya sonuna geldik mi tekrar kontrol et (padding sonrası)
                if (currentPos >= laleExe.Length) break;

                // 5. Bir sonraki string'in uzunluk byte'ını oku ve atla
                // Bu byte'ın değerini kullanmıyoruz ama pozisyonu ilerletmeliyiz.
                byte nextLengthByte = laleExe[currentPos];
                currentPos++;

                // Eğer okunan uzunluk 0 ise, bu genellikle listenin sonu demektir.
                if (nextLengthByte == 0)
                {
                    // Liste sonu belirteci olabilir, döngüden çıkalım.
                    break;
                }

                // currentPos şimdi bir sonraki string'in başlangıcında, döngü devam edecek.
            }

            return list;
        }

        private List<StringInfo> ParseWeaponList2(int startOffset)
        {
            List<StringInfo> list = new List<StringInfo>();
            int pos = startOffset;

            while (pos < laleExe.Length)
            {
                byte length = laleExe[pos];
                pos++;

                if (length == 0)
                    break; // Bitti

                if (pos + length > laleExe.Length)
                    break; // Hatalı veri önlemi

                string text = System.Text.Encoding.ASCII.GetString(laleExe, pos, length);
                int offset = pos - 1; // length byte'ı da dahil edelim

                list.Add(new StringInfo(text, offset));

                pos += length;
                if (pos < laleExe.Length && laleExe[pos] == 0x00)
                    pos++; // Null sonlandırıcıyı atla
                if (pos < laleExe.Length && laleExe[pos] == 0x00)
                    pos++; // Alignment için varsa bir tane daha atla
            }

            return list;
        }

        private static readonly Encoding AmigaEncoding = Encoding.GetEncoding("iso-8859-1");
        private (List<StringInfo> ParsedList, int NextOffset) ParseAmosStyleList(int startOffset)
        {
            List<StringInfo> list = new List<StringInfo>();
            int currentPos = startOffset;

            if (laleExe == null || laleExe.Length == 0 || startOffset < 0 || startOffset >= laleExe.Length)
            {
                return (list, startOffset);
            }

            List<byte> currentStringBytes = new List<byte>();

            while (currentPos < laleExe.Length)
            {
                int currentStringStartOffset = currentPos;
                currentStringBytes.Clear();

                bool foundNull = false;
                while (currentPos < laleExe.Length)
                {
                    byte currentByte = laleExe[currentPos];
                    if (currentByte == 0x00)
                    {
                        foundNull = true;
                        currentPos++;
                        break;
                    }
                    currentStringBytes.Add(currentByte);
                    currentPos++;
                }

                if (!foundNull || (currentStringBytes.Count == 0 && list.Count == 0))
                {
                    if (foundNull && currentStringBytes.Count == 0 && list.Count > 0)
                    {
                        // Potansiyel liste sonu durumu, şimdilik çıkıyoruz.
                    }
                    return (list, startOffset);
                }

                if (currentStringBytes.Count > 0)
                {
                    string text = AmigaEncoding.GetString(currentStringBytes.ToArray());
                    list.Add(new StringInfo(text, currentStringStartOffset));
                }
                else
                {
                    // Boş string okundu, muhtemelen liste sonu veya yapı dışı durum.
                }

                if (currentPos == laleExe.Length) break;

                bool hasPadding = (currentPos % 2 == 0);
                if (hasPadding)
                {
                    if (currentPos < laleExe.Length && laleExe[currentPos] == 0x00)
                    {
                        currentPos++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentPos < laleExe.Length)
                {
                    byte nextLength = laleExe[currentPos];
                    currentPos++;
                    if (nextLength == 0)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return (list, currentPos);
        }

        public void ExportListBoxToTxt(ListBox listBox)
        {
            // openfilereq isimli SaveFileDialog kullanarak kaydetme penceresi açıyoruz
            using (SaveFileDialog openfilereq = new SaveFileDialog())
            {
                openfilereq.Filter = "Metin Dosyası (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*";
                openfilereq.Title = "ListBox içeriğini kaydet";
                openfilereq.FileName = lastfile + ".dim.txt";
                openfilereq.OverwritePrompt = true;

                if (openfilereq.ShowDialog() == DialogResult.OK)
                {
                    // Seçilen dosya yoluna tüm öğeleri satır satır yaz
                    File.WriteAllLines(openfilereq.FileName,
                        listBox.Items
                               .Cast<object>()
                               .Select(item => item.ToString()));
                }
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            ExportListBoxToTxt(listBox1);
        }
    }
}
