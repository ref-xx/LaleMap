using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace LaleMapTest
{
    //2025 ref ^ retrojen.org

    public struct AmigaPic
    {
        public Color[] palette;       // 32 girdilik palet
        public Color[] BackupPalette;       // 32 girdilik palet

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
        public Color[] BackupPalette = new Color[32];
        private LaleMap map = new LaleMap();
        private int searchindex = 0;
        List<byte[]> eventDataList = new List<byte[]>(); // form sınıfına ekle
        int eventOffset = 0;
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
        "Player",
        "irospa",
        "ipna",
        "lavuk",
        "maganda",
        "hIrgIz",
        "bahCIvan",
        "lale savaSCIsI",
        "dadaS",
        "meycik yuzIr",
        "faytIr",
        "kIlerik",
        "ayI",
        "kOpek",
        "guS",
        "fare",
        "yarasa",
        "OmUrcek bOcUGU",
        "dev OmUrcek",
        "yobaz ograten",
        "yobaz beSamel",
        "yobaz bolonez",
        "yobaz napoliten",
        "kelp",
        "karafatma",
        "Cembersakal",
        "fanatik",
        "leS gargasI",
        "dellenmiS OGrenci",
        "dellenmiS OGretmen",
        "balgam",
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
            textBox2.Text += "\r\nOffset:" + eventOffset.ToString();
            byte[] result = File.ReadAllBytes("senaryo\\" + textBox1.Text);
            listEvents(result, eventOffset);
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
                default: return Brushes.White;      // Varsayılan (Boş)
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






        public int drawDungeon(int highlightEvent = -1)
        {
            int minEvent = 999999999;
            if (listBox1.Items.Count < map.width * map.height) // En az 100 oda olmalı
            {
                MessageBox.Show("Eksik oda verisi!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (0);
            }

            // Dungeon boyutları
            int margin = 1; // Oda araları boşluk
            int roomSize = ((pictureBox1.Width - map.width * margin) / map.width); // Her oda 30x30 piksel olacak

            int dungeonSize = map.width; // 10x10 grid

            // Resmi oluştur
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                //g.Clear(Color.White);
                g.FillRectangle(Brushes.LightGray, 0, 0, map.width * margin + roomSize * map.height, map.width * margin + roomSize * map.height);
                Font font = new Font("Arial", 6, FontStyle.Bold);
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                for (int i = 0; i < dungeonSize; i++)
                {
                    for (int j = 0; j < dungeonSize; j++)
                    {
                        int index = i * dungeonSize + j;
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




                        // Odanın x, y konumunu belirle
                        int x = j * (roomSize + margin);
                        int y = i * (roomSize + margin);

                        // **1. Zemin Tipine Göre Odanın Arkasını Doldur**
                        Brush floorBrush = GetFloorBrush(ceilingType);
                        // **2. Duvarları Çiz**
                        ////int i1 = highlightEvent / 32;
                        //int i2 = highlightEvent - (i1 * 32);

                        if (highlightEvent == (roomevent + (roomevent32 - 1) * 31))
                            g.FillRectangle(Brushes.Magenta, x, y, roomSize, roomSize);
                        else
                            g.FillRectangle(floorBrush, x, y, roomSize, roomSize);
                        /*
                                                DrawWall(g, x, y, x + roomSize, y, topWall, topWall); // Üst duvar
                                                DrawWall(g, x + roomSize, y, x + roomSize, y + roomSize, rightWall, rightWall); // Sağ duvar
                                                DrawWall(g, x, y + roomSize, x + roomSize, y + roomSize, bottomWall, bottomWall); // Alt duvar
                                                DrawWall(g, x, y, x, y + roomSize, leftWall, leftWall); // Sol duvar


                                                x += 3;
                                                y += 3;// **2. Duvarları Çiz**
                        */
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
                    }
                }
            }

            pictureBox1.Image = bmp; // Çizimi PictureBox'a aktar
            return minEvent;
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

                return $"Bilinmeyen bank tipi (magic: '{magic}')";
            }
        }

        void parsePic(byte[] fileData)
        {
            PicBank = MakePalette(fileData);
            listBox1.Items.Add((PicBank.width * 8).ToString() + "x" + (PicBank.lumph * PicBank.lumps).ToString());
            PicBank = Decompress(PicBank);
            PicBank = PaintAmigaPic(PicBank);

        }

        // Byte array içinde başka bir byte array arayan metod
        public static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
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
                listBox1.Items.Add("Resim başlığı bulunamadı.");

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


        public AmigaPic PaintAmigaPic(AmigaPic pic)
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
                    if ((i > 19))
                    {
                        if ((fileData[i] > 21) && (fileData[i] < 119)) decryptedBytes[i] = (byte)(fileData[i] + 10); // Her byte'ı 10 artır

                    }
                }

                File.WriteAllBytes(filePath + "out.dat", decryptedBytes);

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
                    case 0x0D:



                        byte blokimg = eventData[offset];
                        offset++;
                        sb.AppendLine(linecounter++.ToString() + "." + "Room Begin: (" + blokimg.ToString() + ")");
                        break;

                    case 0x01:
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




                    case 0x02:
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

                    case 0x03:
                        match = true;
                        if (offset < eventData.Length)
                        {
                            byte imageId = eventData[offset++];
                            sb.AppendLine(linecounter++.ToString() + "." + $"Show Image({imageId})");
                        }
                        break;

                    case 0x04:
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

                    case 0x05:
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
                                sb.AppendLine(linecounter++.ToString() + "." + $"Input?  {q1} / {q2}");
                            }
                        }
                        break;

                    case 0x07:
                        match = true;
                        if (offset + 1 <= eventData.Length)
                        {
                            byte var = eventData[offset++];
                            //byte val = eventData[offset++];
                            //byte val2 = eventData[offset++];
                            //byte val3 = eventData[offset++];
                            sb.AppendLine(linecounter++.ToString() + "." + $"SetVariable ({var})."); //, value ={ val}
                        }
                        break;

                    case 0x08:
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
                            sb.AppendLine(linecounter++.ToString() + "." + $"[startFight: enemyId={enemyId}({enemies[enemyId]}), count={count}, ?: {bilinmeyen2}, {bilinmeyen3}, {bilinmeyen4}, {bilinmeyen5}]");

                        }
                        break;
                    case 0x09:
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

                    case 0x0A:
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

                            // İkinci AC'yi geç (isteğe bağlı)
                            if (offset < eventData.Length && eventData[offset] == 0xAC)
                            {
                                offset++;
                            }
                        }
                        break;

                    case 0x0B:
                        match = true;
                        if (offset + 4 < eventData.Length) // toplam 5 byte
                        {
                            byte bilinmeyen2 = eventData[offset++];
                            byte bilinmeyen3 = eventData[offset++];
                            byte bilinmeyen4 = eventData[offset++];
                            byte bilinmeyen5 = eventData[offset++];
                            sb.AppendLine(linecounter++.ToString() + "." + $"******* Next Map: {bilinmeyen2}, {bilinmeyen3}, {bilinmeyen4}, {bilinmeyen5}");
                        }
                        break;
                    case 0x0E:
                        match = true;
                        if (offset < eventData.Length)
                        {
                            byte dolar = eventData[offset++];
                            sb.AppendLine(linecounter++.ToString() + "." + $"End {dolar}.");
                        }
                        break;

                    case 0x10:

                        sb.AppendLine(linecounter++.ToString() + "." + "[break]");
                        break;

                    case 0x11:
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
                                if (faceId == 0) sb.AppendLine(linecounter++.ToString() + "." + $"Talk Random Face \"{text}\""); else sb.AppendLine(linecounter++.ToString() + "." + $"Talk with face {faceId},\"{text}\"");
                            }
                        }
                        break;

                    case 0x1B:
                        match = true;
                        sb.AppendLine(linecounter++.ToString() + "." + "[wordList]");

                        while (offset < eventData.Length)
                        {
                            byte len = eventData[offset++];

                            if (len == 0x03)
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

                            sb.AppendLine($" - [{page}-{wordNo}]: \"{realWord}\"");

                            if (textEnd < raw.Length && control == 0x03)
                            {
                                sb.AppendLine("[end of word list]");
                                break;
                            }
                        }
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
            BackupPalette[0] = Color.Magenta;
        }
    }
}
