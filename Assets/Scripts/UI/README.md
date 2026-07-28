# UI — önce beni oku

Oyun içi arayüz (mağaza, seviye atlama ekranı, ana menü, ölüm ekranı, HUD barları)
**çalışma zamanında, koddan** oluşturulur ve biçimlendirilir — sahnede (Scene)
ayarlanmaz.

## "Scene görünümünde Canvas neden çirkin renkli kutular gibi görünüyor?"

Bu normal. `DemoScene`'i aç; Canvas yer tutucu yeşil/mavi/kırmızı panellerden ve
her yerde "New Text" yazısından oluşur. **Play**'e bastığında aşağıdaki scriptler
her şeyi bu yer tutucuların üzerine yeniden konumlandırır, renklendirir ve dizer.

**Arayüzü Scene görünümünde değil, Play modunda değerlendir.** O panelleri sahnede
düzenlemek anlamsız — Play'e her bastığında kod onların üzerine yazar.

## Neyi nereden değiştirirsin

| Değiştirmek istediğin... | Şunu düzenle |
|---|---|
| Herhangi bir renk (yüzeyler, yazı, altın, nadirlik) | `UITheme.cs` — tüm arayüzün tek renk paleti |
| Yuvarlak şekiller / köşe yarıçapı | `UIStyle.cs` — kodda çizilen sprite'lar, resim dosyası yok |
| Seviye atlama ekranı (kartlar, yeniden atma, önizleme) | `UIManager.StyleLevelUpCards()` / `LayoutCard()` / `Bind()` |
| Can / XP barları (boyut, konum, renk) | `UIManager.StyleHealthBar()` / `StyleXPBar()` / `StyleSliderChrome()` |
| Mağaza gridi (kartlar, satın alma, yerleşim) | `ShopManager.StyleShop()` / `LayoutShopCard()` / `RefreshShopCards()` |
| Ana menü butonları | `GameManager` (menü buton biçimlendirmesi) |

Her `Style...` metodu ilgili bileşenin `Start()` metodundan çağrılır. Sahne
değişikliklerinin ezilmesinin sebebi budur — sahneyi değil, kodu değiştir.

### HUD bar ayarları (hızlı referans)

`UIManager.cs` içinde:
- Can barı: `StyleHealthBar()` içindeki `sizeDelta` / `anchoredPosition`
- XP barı: `StyleXPBar()` içindeki `sizeDelta` (yükseklik = kalınlık)
- Renkler: her metottan `StyleSliderChrome(...)`'a geçirilen `Color(...)`

## Oyun hissi / juice

`../Managers/JuiceManager.cs`; hasar sayılarını, hit-stop'u, düşman ölüm
parçacıklarını ve oyuncu hasar aldığında kırmızı ekran parlamasını yönetir.
**Kendini çalışma zamanında oluşturur** (`[RuntimeInitializeOnLoadMethod]` ile) —
yani sahnede DEĞİLDİR, aramaya kalkma. Her yerden statik metotlarla çağır:
`JuiceManager.DamageNumber / HitStop / DeathPop / Shake / PlayerHitFlash`.
Ekran sarsıntısı `../Camera/TopDownCameraFollow.AddShake()` içindedir.
