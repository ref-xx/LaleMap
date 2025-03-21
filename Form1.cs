using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

namespace LaleMapTest
{
    //2025 ref ^ retrojen.org

    public struct AmigaPic
    {
        public Color[] palette;       // 32 girdilik palet
        public int width;             // Genişlik (dosyada bayt cinsinden)
        public int height;            // Yükseklik (satır sayısı)
        public int lumps;             // Linelumps (ll)
        public int dlumps;            // Bitplane sayısı (d)
        public byte[] pixelData;      // MakePalette'de kopyalanan sıkıştırılmış veri (dosyanın o+24'ten sonrası)
        public byte[] uncompressedData; // Çözülen ham piksel verisi

        // Aşağıdaki iki alan, RLE dekompresyonunda ihtiyaç duyulan kontrol veri bloklarının,
        // pic.pixelData içindeki başlangıç offsetlerini temsil eder.
        public int rleDataOffset;
        public int pointsOffset;

        public byte[,] indexedImage; //actual amiga image WxH
    }

    public struct LaleMap
    {
        public int startx;
        public int starty;


        public int width;             // Genişlik (dosyada bayt cinsinden)
        public int height;            // Yükseklik (satır sayısı)
    }

    public partial class Form1 : Form
    {
        private AmigaPic PicBank = new AmigaPic();
        private LaleMap map = new LaleMap();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            map=ListData(PicBank);

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
            if (listBox1.Items.Count < map.width*map.height) // En az 100 oda olmalı
            {
                MessageBox.Show("Eksik oda verisi!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Dungeon boyutları
            int roomSize = (pictureBox1.Width/map.width)-3; // Her oda 30x30 piksel olacak
            int margin = 0; // Oda araları boşluk
            int dungeonSize = map.width; // 10x10 grid

            // Resmi oluştur
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
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
                        if (index >= listBox1.Items.Count) continue;

                        string selectedLine = listBox1.Items[index].ToString();
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

                        // Odanın x, y konumunu belirle
                        int x = j * (roomSize + margin);
                        int y = i * (roomSize + margin);

                        // **1. Zemin Tipine Göre Odanın Arkasını Doldur**
                        Brush floorBrush = GetFloorBrush(ceilingType);
                        g.FillRectangle(floorBrush, x, y, roomSize, roomSize);

                        // **2. Duvarları Çiz**
                        DrawWall(g, x, y, x + roomSize, y, topWall, topType); // Üst duvar
                        DrawWall(g, x + roomSize, y, x + roomSize, y + roomSize, rightWall, rightType); // Sağ duvar
                        DrawWall(g, x, y + roomSize, x + roomSize, y + roomSize, bottomWall, bottomType); // Alt duvar
                        DrawWall(g, x, y, x, y + roomSize, leftWall, leftType); // Sol duvar

                        // **3. Tavan Tipine Göre Oda İndeks Rengini Ayarla**
                        Brush textBrush = GetCeilingBrush(floorType);

                        // **4. Oda Index'ini Ortaya Yaz**
                        g.DrawString((index + 1).ToString(), font, textBrush, new RectangleF(x, y, roomSize, roomSize), sf);
                    }
                }
            }

            pictureBox1.Image = bmp; // Çizimi PictureBox'a aktar
        }



        private void DrawWall(Graphics g, int x1, int y1, int x2, int y2, int wallExists, int wallType)
        {
            if (wallExists == 0) return; // Eğer duvar yoksa çizme

            Pen pen = new Pen(Color.Black, 3); // Varsayılan siyah

            switch (wallType)
            {
                case 2:
                    pen.Color = Color.Black; // Duvar
                    break;
                case 3:
                    pen.Color = Color.Green; // Kapı 
                    break;
                case 4:
                    pen.Color = Color.Red; // Ana Çıkış
                    break;
                default:
                    pen.Color = Color.Blue; // Bu nedir bu
                    break;
            }

            g.DrawLine(pen, x1, y1, x2, y2);
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
            listBox1.Items.Clear();
            listBox1.Items.Add(CheckBankType(textBox1.Text));
            
        }


        public string CheckBankType(string filename)
        {
            // Dosya var mı kontrol et
            if (!File.Exists(filename))
                return "Dosya bulunamadı.";

            // Dosya içeriğini oku
            byte[] fileData = File.ReadAllBytes(filename);
            if (fileData.Length < 20)
                return "Geçersiz dosya: Yeterli veri yok.";

            // İlk 4 baytı okuyarak magic değeri elde et
            string magic = Encoding.ASCII.GetString(fileData, 0, 4);

            // Eğer bank dosyası T_AMBK ise offset 12’den bank adını al
            if (magic == "AmBk")
            {
                string bankName = Encoding.ASCII.GetString(fileData, 12, 8).Trim();

                // Bank adını kontrol ederek hangi T_AMBK tipi olduğunu belirle
                if (bankName.Equals("Pac.Pic.", StringComparison.OrdinalIgnoreCase))
                {
                    PicBank = MakePalette(fileData);
                    listBox1.Items.Add((PicBank.width*8) .ToString() + "x" + (PicBank.height*PicBank.lumps).ToString());
                    PicBank=Decompress(PicBank);
                    PicBank = PaintAmigaPic(PicBank);

                    return "Pac.Pic.";
                }
                else if (bankName.Equals("Samples", StringComparison.OrdinalIgnoreCase))
                    return "Samples";
                else if (bankName.Equals("Work", StringComparison.OrdinalIgnoreCase))
                    return "Work";
                else if (bankName.Equals("Datas", StringComparison.OrdinalIgnoreCase))
                    return "Datas";
                else
                    return $"Bilinmeyen T_AMBK tipi: '{bankName}'";
            }
            else if (magic == "AMSP")
            {
                return "T_AMSP (Sprite Bank)";
            }
            else if (magic == "AMIC")
            {
                return "T_AMIC (Icon Bank)";
            }
            else
            {
                return $"Bilinmeyen bank tipi (magic: '{magic}')";
            }
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
                throw new Exception("Dosya boyutu yetersiz.");

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
                        throw new Exception("Palet verileri eksik.");
                    }
                }
                // Palet okunduktan sonra, offset'i resim başlığına ilerletmek için güncelliyoruz
                o += 90;
            }
            else
            {
                throw new Exception("HAM başlığı bulunamadı, dosya formatı beklenenden farklı.");
            }

            // Resim başlığının kontrolü: Beklenen magic değeri 0x06071963
            if (fileData.Length < o + 24)
                throw new Exception("Dosya boyutu yetersiz, resim başlığı eksik.");

            uint picHeader = (uint)((fileData[o] << 24) | (fileData[o + 1] << 16) | (fileData[o + 2] << 8) | fileData[o + 3]);
            if (picHeader != 0x06071963)
                throw new Exception("Resim başlığı bulunamadı.");

            // Genişlik ve yüksekliği oku (big-endian: 2 bayt)
            pic.width = ((fileData[o + 8] << 8) | fileData[o + 9]);
            pic.height = (fileData[o + 10] << 8) | fileData[o + 11];

            // Lumps (ll) ve Bitplane Sayısı (dlumps) Okuma
            pic.lumps = (fileData[o + 12] << 8) | fileData[o + 13]; // ll (linelumps)
            pic.dlumps = (fileData[o + 14] << 8) | fileData[o + 15]; // d (bitplane sayısı)

            pic.rleDataOffset = get4(fileData, o + 16) - 24;
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
            int h = pic.height;     // Yükseklik (satır sayısı)
            int ll = pic.lumps;     // Linelumps sayısı
            int d = pic.dlumps;     // Bitplane (dlumps) sayısı

            // Çözülen (uncompressed) veri boyutu:
            int totalSize = w * h * ll * d;
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
            // d: bitplane sayısı, h: satır sayısı, w: sütun sayısı, ll: lump (linelumps) sayısı
            for (int i = 0; i < d; i++)
            {
                // Her bitplane için, uncompressed verideki başlangıç index'i:
                int planeOffset = i * (w * h * ll);

                for (int j = 0; j < h; j++)
                {
                    // j. satırın lump bölümünün başlangıcı:
                    int rowOffset = planeOffset + j * (w * ll);

                    for (int k = 0; k < w; k++)
                    {
                        // k. sütunun lump içindeki başlangıcı:
                        int baseIndex = rowOffset + k;

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
                                    rlebyte = pic.pixelData[rledataPos++];
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
        int finalHeight = pic.height * pic.lumps;
        int w = pic.width;    // satırdaki byte sayısı
        int h = pic.height;   // scanline grubu sayısı
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
            string headerLine = "";
            for (int x = 0; x < 20; x++)
            {
                headerLine += pic.indexedImage[x, 0].ToString() + "\t";
            }
            //listBox1.Items.Add(headerLine);

            // --- 2. Data Length Hesaplama ---
            // indexedImage[4,0] ve [5,0] sırasıyla yatay ve dikey değeri içeriyor.
            int horizontal = pic.indexedImage[4, 0];
            int vertical = pic.indexedImage[5, 0];
            int dataLength = horizontal * vertical;
            map.width = horizontal;
            map.height = vertical;

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
                    int endColumn = Math.Min(currentColumn + 20, imageWidth);
                    string line = "Idx:" + y.ToString(); // Her satırın başına "Idx:" ekle

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


    }
}
