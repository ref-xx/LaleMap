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
        private LaleMap map = new LaleMap();
        private int searchindex = 0;
        List<byte[]> eventDataList = new List<byte[]>(); // form sınıfına ekle

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            map = ListData(PicBank);

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






        public void drawDungeon()
        {
            if (listBox1.Items.Count < map.width * map.height) // En az 100 oda olmalı
            {
                MessageBox.Show("Eksik oda verisi!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Dungeon boyutları
            int margin = 1; // Oda araları boşluk
            int roomSize = ((pictureBox1.Width - map.width * margin) / map.width); // Her oda 30x30 piksel olacak

            int dungeonSize = map.width; // 10x10 grid

            // Resmi oluştur
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.FillRectangle(Brushes.Black, 0, 0, map.width * margin + roomSize * map.height, map.width * margin + roomSize * map.height);
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



                        // Odanın x, y konumunu belirle
                        int x = j * (roomSize + margin);
                        int y = i * (roomSize + margin);

                        // **1. Zemin Tipine Göre Odanın Arkasını Doldur**
                        Brush floorBrush = GetFloorBrush(ceilingType);
                        // **2. Duvarları Çiz**
                        g.FillRectangle(floorBrush, x, y, roomSize, roomSize);

                        DrawWall(g, x, y, x + roomSize, y, topWall, topWall); // Üst duvar
                        DrawWall(g, x + roomSize, y, x + roomSize, y + roomSize, rightWall, rightWall); // Sağ duvar
                        DrawWall(g, x, y + roomSize, x + roomSize, y + roomSize, bottomWall, bottomWall); // Alt duvar
                        DrawWall(g, x, y, x, y + roomSize, leftWall, leftWall); // Sol duvar


                        x += 3;
                        y += 3;// **2. Duvarları Çiz**

                        DrawWall(g, x, y, x + roomSize, y, topWall, topType); // Üst duvar
                        DrawWall(g, x + roomSize, y, x + roomSize, y + roomSize, rightWall, rightType); // Sağ duvar
                        DrawWall(g, x, y + roomSize, x + roomSize, y + roomSize, bottomWall, bottomType); // Alt duvar
                        DrawWall(g, x, y, x, y + roomSize, leftWall, leftType); // Sol duvar

                        // **3. Tavan Tipine Göre Oda İndeks Rengini Ayarla**
                        Brush textBrush = GetCeilingBrush(floorType);
                        Brush eventBrush = Brushes.Yellow;

                        // **4. Oda Index'ini Ortaya Yaz**
                        g.DrawString((index).ToString(), font, textBrush, new RectangleF(x, y, roomSize, roomSize), sf);
                        if (roomevent > 0) g.DrawString(roomevent.ToString(), font, eventBrush, new RectangleF(x, y - 10, roomSize, roomSize), sf);
                    }
                }
            }

            pictureBox1.Image = bmp; // Çizimi PictureBox'a aktar
        }



        private void DrawWall(Graphics g, int x1, int y1, int x2, int y2, int wallExists, int wallType)
        {
            if (wallExists == 0) return;

            Pen pen = new Pen(Color.Black, 2);
            Pen duvar = new Pen(Color.Black, 2);
            switch (wallType)
            {
                case 1:
                    Pen pene = new Pen(Color.Blue, 1);
                    g.DrawLine(pene, x1, y1, x2, y2);
                    break;
                case 3:
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

                case 2:
                    pen.Color = Color.Black; // Başka tür bir kapıysa düz çizgi
                    g.DrawLine(pen, x1, y1, x2, y2);
                    break;

                case 4:
                    pen.Color = Color.Cyan;
                    g.DrawLine(pen, x1, y1, x2, y2);
                    break;

                default:
                    pen.Color = Color.Black;
                    g.DrawLine(pen, x1, y1, x2, y2);
                    break;
            }
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



        private void listEvents(byte[] storyData)
        {
            listBox2.Items.Clear();

            if (storyData == null || storyData.Length < 22)
            {
                listBox2.Items.Add("Veri geçersiz veya eksik.");
                return;
            }

            int offset = 20; // İlk 20 byte: header

            // Event count (big-endian)
            ushort eventCount = ReadWordBigEndian(storyData, offset);
            offset += 2;

            listBox2.Items.Add("Event Count: " + eventCount);

            for (int i = 0; i < eventCount; i++)
            {
                if (offset + 2 > storyData.Length)
                {
                    listBox2.Items.Add($"Event #{i + 1}: Başlık okunamıyor (veri bitti)");
                    break;
                }

                ushort eventLength = ReadWordBigEndian(storyData, offset);
                offset += 2;

                if (offset + eventLength > storyData.Length)
                {
                    listBox2.Items.Add($"Event #{i + 1}: Veri eksik (uzunluk={eventLength})");
                    break;
                }

                byte[] eventData = new byte[eventLength];
                Array.Copy(storyData, offset, eventData, 0, eventLength);
                offset += eventLength;

                string eventText = DecipherROT(eventData); // ya da Encoding.ASCII.GetString(DecipherROT(...))

                eventDataList.Add(eventData); // listBox2.Items.Add(...)'dan önce

                listBox2.Items.Add($"Event #{i + 1} ({eventLength} bayt)");

                //listBox2.Items.Add($"Event #{i + 1} ({eventLength} bayt): {eventText}");
            }
        }

        // Yardımcı fonksiyon
        private ushort ReadWordBigEndian(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }








        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                drawRoom(listBox1.SelectedIndex);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            drawDungeon();
        }

        private void button2_Click(object sender, EventArgs e)
        {

            string filename = "haritalar\\" + textBox1.Text;
            // Dosya var mı kontrol et
            if (!File.Exists(filename))
                return;

            // Dosya içeriğini oku
            byte[] fileData = File.ReadAllBytes(filename);
            if (fileData.Length < 20)
                return;


            listBox1.Items.Clear();
            listBox1.Items.Add(CheckBankType(fileData));
            byte[] result = File.ReadAllBytes("senaryo\\" + textBox1.Text);
            listEvents(result);

            //textBox2.Text = map.scenario;
            //File.WriteAllBytes("senaryo\\1out.dat", result);

            //List<string> results = AnalyzeBank(("senaryo\\" + textBox1.Text));
            //listBox1.Items.Clear();
            //listBox1.Items.AddRange(results.ToArray()); 
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

            byte[] data = eventDataList[index - 1];
            textBox2.Text = ViewEvent(data);
            textBox2.Text += ViewEvent2(data);
        }

        private string ViewEvent(byte[] eventData)
        {
            StringBuilder sb = new StringBuilder();
            int offset = 0;

            // İlk 8 baytı atla
            if (eventData.Length < 9) return "[Geçersiz event]";
            offset = 8;

            while (offset < eventData.Length)
            {
                byte cmd = eventData[offset++];

                switch (cmd)
                {
                    case 0x0D:



                        byte blokimg = eventData[offset];
                        offset++;
                        sb.AppendLine("[Start Block: Image(" + blokimg.ToString() + ")]");
                        break;

                    case 0x01:
                        if (offset + 3 <= eventData.Length)
                        {
                            byte var = eventData[offset++];
                            byte equals = eventData[offset++];
                            byte jumpOver = eventData[offset++];
                            //byte jumpElse = eventData[offset++];
                            if (var == 1) sb.AppendLine($"IF isSet({equals}) Then Go To {jumpOver} Else: ");//,  else={jumpElse}]");
                            else sb.AppendLine($"IF Operation({var}) -> ({equals}) Then Go To {jumpOver} Else: ");
                        }
                        break;

                    case 0x02:
                        if (offset < eventData.Length)
                        {
                            byte len = eventData[offset++];
                            if (offset + len <= eventData.Length)
                            {
                                byte[] txt = new byte[len];
                                Array.Copy(eventData, offset, txt, 0, len);
                                string text = DecipherROT(txt).Replace("\xAC", " ");
                                sb.AppendLine($"Print \"{text}\"");
                                offset += len;
                            }
                        }
                        break;

                    case 0x03:
                        if (offset < eventData.Length)
                        {
                            byte imageId = eventData[offset++];
                            sb.AppendLine($"Show Image({imageId})");
                        }
                        break;

                    case 0x04:
                        if (offset < eventData.Length)
                        {
                            //byte roomId = eventData[offset++];
                            sb.AppendLine($"Enter Room");
                        }
                        break;

                    case 0x05:
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
                                sb.AppendLine($"[askQuestion]\n - {q1}\n - {q2}");
                            }
                        }
                        break;

                    case 0x07:
                        if (offset + 4 <= eventData.Length)
                        {
                            byte var = eventData[offset++];
                            byte val = eventData[offset++];
                            byte val2 = eventData[offset++];
                            byte val3 = eventData[offset++];
                            sb.AppendLine($"[setVariable: var={var}, value={val}, Unk={val2}, Unk={val3}]");
                        }
                        break;

                    case 0x08:
                        if (offset + 3 <= eventData.Length)
                        {
                            byte zero = eventData[offset++];
                            byte enemyId = eventData[offset++];
                            byte count = eventData[offset++];
                            sb.AppendLine($"[startFight: enemyId={enemyId}, count={count}]");
                        }
                        break;

                    case 0x0E:
                        if (offset < eventData.Length)
                        {
                            //offset++;
                            sb.AppendLine("End.");
                        }
                        break;

                    case 0x10:
                        sb.AppendLine("[break]");
                        break;

                    case 0x11:
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
                                sb.AppendLine($"[displayTextWithFace: face={faceId}]: {text}");
                            }
                        }
                        break;

                    default:
                        // Bilinmeyen komut ya da düz veri — ASCII kontrolü
                        sb.Append($"[0x{cmd:X2}] ");
                        break;
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

                    MessageBox.Show("Dump tamamlandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
