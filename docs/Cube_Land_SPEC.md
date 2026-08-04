# SPEC – CUBE LAND
### Tài liệu đặc tả thiết kế game (Game Design Document)
**Thể loại:** Puzzle giải trí 3D – Voxel Color-Match / "Block Blast"
**Phiên bản tài liệu:** 1.0
**Ngày:** 15/07/2026
**Tham khảo thị trường:** *Cube Land Puzzle Game* (Rotatelab Yazilim ve Bilisim A.S., package `com.rotatelab.cubeblast`, Google Play/App Store, rating 4.5★, 100K–500K lượt cài, thể loại Giải đố)

---

## 0. Tóm tắt nghiên cứu game tham khảo

| Thuộc tính | Ghi nhận |
|---|---|
| Cơ chế lõi | Bắn "súng màu" (blaster) vào một khối cấu trúc voxel 3D; khối nào trùng màu với blaster sẽ vỡ/biến mất |
| Điều khiển | Một chạm (one-tap), không cần kéo thả phức tạp |
| Áp lực thời gian | Không có đồng hồ đếm ngược – chơi theo tốc độ riêng |
| Camera | Khối cấu trúc xoay được (rotate) để người chơi ngắm và nhắm vào các mặt bị khuất |
| Số lượng màn | Hàng trăm màn thiết kế thủ công, độ khó tăng dần |
| Nền tảng | Android, iOS, chơi offline hoàn toàn (không cần wifi) |
| Kiếm tiền | Quảng cáo giữa các màn + rewarded ads + IAP (gỡ quảng cáo, gói booster) |
| Điểm được khen | Hiệu ứng phá vỡ mãn nhãn, cảm giác vật lý khối "chắc chắn", độ khó tăng hợp lý, đồ hoạ voxel tươi sáng |
| Điểm bị phàn nàn (từ review thật) | (1) Lỗi đếm sai số lượng khối theo màu khiến màn không thể giải được; (2) tự động hoàn thành nốt các bước cuối, tước mất cảm giác tự tay hoàn thành; (3) mở app lại buộc chơi lại màn vừa xong; (4) sau ~màn 300 lặp lại đúng 10 màn cũ; (5) từ màn ~165 trở đi gần như không thể thắng nếu không mua booster, không có cách xem quảng cáo đổi booster |

→ Các điểm bị phàn nàn ở trên được đưa thẳng vào mục **Nguyên tắc thiết kế bắt buộc** và **Khác biệt hoá** bên dưới, để Cube Land của chúng ta tránh lặp lại.

---

## 1. Tổng quan trò chơi

- **Tên game:** Cube Land
- **Elevator pitch:** *"Ngắm đúng màu, bắn đúng lúc – từng khối lập phương sụp đổ trong một câu đố 3D thư giãn, không giới hạn thời gian."*
- **Thể loại:** Casual puzzle 3D, color-match / destruction puzzle
- **Nền tảng:** Android & iOS (ưu tiên mobile, thiết kế chơi một tay)
- **Đối tượng người chơi:** Mọi lứa tuổi (rating 3+), người thích giải trí ngắn, không áp lực, thích cảm giác "đã tay" khi phá vỡ vật thể
- **Model doanh thu:** Free-to-play, quảng cáo + IAP nhẹ
- **Điểm khác biệt cốt lõi so với game tham khảo:** công bằng hơn ở giai đoạn khó, không tự động chơi hộ người dùng, đa dạng theme "Land" (thế giới) thay vì một bối cảnh xuyên suốt

---

## 2. Vòng lặp gameplay chính (Core Loop)

```
Vào màn → Quan sát khối cấu trúc (xoay 360°) → Lên kế hoạch thứ tự bắn
   → Chạm blaster màu để bắn → Khối trùng màu vỡ & rơi/biến mất
   → Cấu trúc settle lại (lộ khối mới bị che khuất) → Lặp lại
   → Hết khối = Thắng (nhận sao + coin) → Màn tiếp theo
   → Hết blaster mà còn khối = Thua → Thử lại / dùng booster / xem quảng cáo
```

Một ván trung bình kéo dài **30–90 giây**, phù hợp phiên chơi ngắn (giải lao, xếp hàng, trước khi ngủ).

---

## 3. Cơ chế chi tiết

### 3.1 Cấu trúc khối (Cube Structure)
- Cấu trúc được tạo từ các **voxel đơn vị** (khối lập phương nhỏ, phong cách pixel-art 3D), mỗi voxel mang 1 màu.
- Hình dạng đa dạng theo độ khó: khối lập phương đặc, kim tự tháp, hình cầu xấp xỉ (sphere voxel), chữ cái/số, con vật cách điệu, biểu tượng theo mùa lễ hội…
- Một số voxel bị **khuất phía sau/trong** cấu trúc → bắt buộc người chơi xoay để lộ ra và nhắm bắn.

### 3.2 Blaster màu (Color Blasters)
- Mỗi màn chơi cấp một **hàng đợi blaster** giới hạn, mỗi blaster gồm: `màu` + `số phát bắn`.
- Người chơi chạm vào blaster đang active để bắn 1 phát vào khối cùng màu gần nhất/được nhắm.
- Cơ chế lựa chọn (cần quyết định thiết kế – xem mục 3.5):
  - **Phương án A – Hàng đợi tuần tự:** chỉ blaster đầu hàng được bắn, bắn xong tự chuyển sang blaster kế; buộc người chơi lên kế hoạch trước.
  - **Phương án B – Tự do chọn:** hiển thị vài blaster cùng lúc, người chơi chọn thứ tự bắn tuỳ ý (linh hoạt hơn, dễ tiếp cận hơn).
- **Khuyến nghị:** dùng **Phương án B** ở 60 màn đầu (làm quen), chuyển dần sang **Phương án A** ở màn khó để tăng chiều sâu chiến thuật.

### 3.3 Camera & thao tác xoay
- Kéo ngang/dọc màn hình để xoay cấu trúc 360° quanh trục.
- Zoom bằng pinch (tuỳ chọn).
- Auto-rotate nhẹ nhàng khi người chơi không thao tác, để gợi ý còn khối ẩn.

### 3.4 Điều kiện Thắng/Thua
- **Thắng:** toàn bộ voxel bị loại bỏ trước khi hết blaster.
- **Thua:** hết blaster mà cấu trúc còn khối → màn hình thua, cho phép: chơi lại, xem quảng cáo đổi 1 blaster bù, hoặc dùng booster đã có.
- **Nguyên tắc thiết kế bắt buộc (rút ra từ lỗi của game tham khảo):**
  > Tổng số voxel mỗi màu trong toàn bộ cấu trúc (kể cả phần bị khuất) **phải bằng chính xác** tổng số phát bắn của blaster màu đó. Mọi màn chơi – thủ công hay sinh tự động – đều phải chạy qua **bộ kiểm định (level validator)** trước khi đưa vào game để đảm bảo không có màn bị lệch số lượng (đây là lỗi được người chơi phản ánh nhiều nhất ở game tham khảo).

### 3.5 Vật lý & hiệu ứng phá vỡ
- Khi voxel bị bắn trúng: hiệu ứng vỡ hạt (particle) theo màu, voxel biến mất hoặc rơi theo trọng lực nếu mất điểm tựa.
- Cấu trúc "settle" (các khối phía trên rơi xuống lấp chỗ trống) để tạo cảm giác vật lý thật và lộ khối mới.

---

## 4. Tiến trình & độ khó (Progression)

| Giai đoạn | Số màn (đề xuất) | Đặc điểm |
|---|---|---|
| Giới thiệu | 1–20 | 2–3 màu, hình khối đơn giản, blaster dư dả, tự do chọn thứ tự |
| Làm quen chiến thuật | 21–60 | 3–4 màu, xuất hiện khối bị che khuất, cần xoay |
| Trung bình | 61–150 | 4–6 màu, blaster khít số lượng, xuất hiện khối đặc biệt (xem 4.1) |
| Khó | 151–300 | Nhiều lớp che khuất, blaster hàng đợi tuần tự, kết hợp nhiều loại khối đặc biệt |
| Siêu khó / Master | 300+ | Bố cục phức tạp, đòi hỏi lập kế hoạch nhiều bước, vẫn phải **có thể thắng bằng kỹ năng thuần**, không ép mua booster mới thắng được |

### 4.1 Khối đặc biệt (đề xuất thêm để tăng chiều sâu, khác biệt hoá)
- **Khối khoá (Locked block):** cần trúng 2 phát cùng màu mới vỡ.
- **Khối đa sắc (Multi-color):** đổi màu ngẫu nhiên sau mỗi lượt bắn trượt, buộc tính toán thời điểm bắn.
- **Khối bom màu (Color bomb):** khi vỡ, phá luôn các voxel liền kề cùng màu.
- **Khối băng (Frozen):** phải phá lớp băng bọc ngoài (1 phát bất kỳ màu) trước khi lộ màu thật bên trong.

### 4.2 Nguyên tắc chống lặp màn & chống bí (rút kinh nghiệm từ game tham khảo)
- Ngân hàng màn (level bank) tối thiểu 400–500 màn thủ công trước khi cho phép màn sinh tự động (procedural) chen vào, tránh tình trạng "lặp lại đúng 10 màn cũ" sau vài trăm màn.
- Với màn sinh tự động: bắt buộc chạy **auto-solver** mô phỏng để đảm bảo màn giải được, đúng như quy tắc ở mục 3.4.
- Không thiết kế "tường khó" (difficulty wall) chỉ vượt qua được nhờ mua booster – luôn có đường thắng bằng kỹ năng + booster miễn phí kiếm được qua chơi/quảng cáo.

---

## 5. Hệ thống tiến trình & phần thưởng

- **Sao (1–3 sao/màn):** dựa trên số blaster dư/không dùng đến hoặc không cần booster.
- **Coin:** thưởng sau mỗi màn, dùng mua booster trong shop.
- **Bản đồ thế giới (World Map) theo "Land":** chia màn theo các vùng chủ đề (Forest Land, Ocean Land, Ice Land, Space Land…), mỗi vùng đổi skin voxel/hiệu ứng vỡ, tạo cảm giác mới mẻ liên tục – đây là điểm khác biệt so với game tham khảo (vốn dùng bối cảnh xuyên suốt, ít thay đổi).
- **Thử thách hàng ngày (Daily Puzzle):** 1 màn đặc biệt mỗi ngày, thưởng coin/booster, khuyến khích quay lại.
- **Điểm danh (Login streak):** phần thưởng tăng dần theo số ngày liên tiếp.

### Bảng Booster đề xuất

| Booster | Chức năng | Cách nhận |
|---|---|---|
| Thêm 1 phát bắn | Cộng thêm 1 shot cho 1 blaster màu bất kỳ đang thiếu | Coin hoặc xem rewarded ad |
| Hoàn tác (Undo) | Lùi lại 1 bước bắn | Coin |
| Gợi ý (Hint) | Gợi ý thứ tự bắn tối ưu tiếp theo | Coin hoặc xem rewarded ad |
| Xoá 1 khối chỉ định | Loại bỏ 1 voxel bất kỳ không cần trùng blaster | Coin (giá cao hơn) |

---

## 6. Kiếm tiền (Monetization)

- **Interstitial ads:** hiển thị **giữa các màn** (ví dụ sau mỗi 2–3 màn hoàn thành) – **không bao giờ chèn ads giữa lúc đang chơi một màn** (bài học trực tiếp từ phản hồi tích cực của người chơi với game tham khảo về điểm này).
- **Rewarded video ads:** đổi lấy booster, thêm 1 blaster khi thua, nhân đôi coin sau màn.
- **IAP:**
  - Gỡ quảng cáo (one-time)
  - Gói coin (nhiều mức giá)
  - Gói booster theo combo
  - Gói "No-Ads + VIP" (gỡ quảng cáo + coin hàng ngày + bản đồ độc quyền)
- Nguyên tắc: monetize không được chặn đường thắng bằng kỹ năng – booster là **hỗ trợ**, không phải **điều kiện bắt buộc** để qua màn khó.

---

## 7. Luồng UX/UI

1. **Splash screen** → logo Cube Land
2. **Main Menu:** Chơi tiếp (Play), Bản đồ (World Map), Cửa hàng (Shop), Cài đặt, Điểm danh hàng ngày
3. **Level select / World map:** hiển thị các "Land" theo chủ đề, màn đã qua có sao
4. **Màn hình chơi (Gameplay HUD):**
   - Góc trên: số màn, nút Pause/Cài đặt
   - Giữa: khối cấu trúc 3D (xoay được)
   - Dưới: hàng đợi blaster + nút booster (Hint, Undo, +1 shot)
   - Không có đồng hồ đếm ngược
5. **Màn hình Thắng:** số sao, coin nhận được, nút "Màn tiếp theo", nút chia sẻ
6. **Màn hình Thua:** nút Thử lại, nút "Xem quảng cáo nhận thêm 1 phát bắn", nút dùng booster đã có

**Sửa lỗi UX từ game tham khảo (bắt buộc):**
- Không tự động chơi hộ các bước cuối cùng của người chơi – luôn để người chơi tự bắn phát cuối.
- Khi mở lại app, vào thẳng màn **tiếp theo chưa chơi**, không bắt chơi lại màn vừa hoàn thành.
- Có nút tắt/bật hiệu ứng "auto-finish" nếu tính năng này được giữ lại dưới dạng tuỳ chọn.

---

## 8. Định hướng mỹ thuật (Art Direction)

- Phong cách **voxel pixel-art 3D** tươi sáng, màu sắc bão hoà cao, tương tự khối Minecraft-style nhưng bo góc nhẹ, có shading mềm (ambient occlusion, rim light).
- Hiệu ứng vỡ: hạt (particle) bay theo màu khối, có squash & stretch khi va chạm, rung màn hình nhẹ (subtle screen shake) để tăng cảm giác "đã tay".
- UI tối giản, bo tròn, nút bấm lớn – tối ưu cho thao tác một tay.
- Mỗi "Land" (thế giới) có bảng màu và skin voxel riêng để tạo cảm giác mới mẻ khi tiến bộ.

---

## 9. Âm thanh

- Nhạc nền: lo-fi/ambient nhẹ nhàng, lặp vòng, âm lượng thấp không gây xao nhãng.
- SFX: tiếng "pop/shatter" khi khối vỡ (đổi cao độ theo combo liên tiếp), tiếng chuông nhẹ khi hoàn thành màn, tiếng tap UI gọn.
- Có nút tắt riêng nhạc nền / SFX trong Cài đặt.

---

## 10. Yêu cầu kỹ thuật

- **Engine đề xuất:** Unity 3D (URP) – phù hợp render voxel, particle, vật lý rơi khối; hoặc Godot 4 nếu ưu tiên gọn nhẹ.
- **Nền tảng:** Android (API 24+), iOS (13+).
- **Offline-first:** toàn bộ dữ liệu màn chơi đóng gói sẵn trong app, chơi được hoàn toàn không cần mạng; quảng cáo/IAP mới cần mạng.
- **Lưu trữ:** local save (PlayerPrefs/JSON) + cloud save tuỳ chọn qua Google Play Games / Game Center để đồng bộ đa thiết bị.
- **Hiệu năng mục tiêu:** 60fps trên thiết bị tầm trung; dùng object pooling cho particle & voxel để tối ưu.
- **Định dạng dữ liệu màn chơi (đề xuất, JSON):**

```json
{
  "level_id": 42,
  "shape": "pyramid",
  "voxels": [
    { "pos": [0,0,0], "color": "red" },
    { "pos": [1,0,0], "color": "blue" }
  ],
  "blasters": [
    { "color": "red", "shots": 6 },
    { "color": "blue", "shots": 4 }
  ],
  "mode": "queue_sequential",
  "star_thresholds": { "3": 0, "2": 2, "1": 4 }
}
```
- Cấu trúc JSON này cho phép: (1) đội thiết kế màn tạo level bằng công cụ ngoài rồi export, (2) chạy validator tự động kiểm tra tổng `voxels` theo màu khớp tổng `shots` theo màu trước khi build vào game.

---

## 11. Đội ngũ & phạm vi triển khai (đề xuất)

| Vai trò | Trách nhiệm |
|---|---|
| Game designer / Level designer | Thiết kế độ khó, tạo & kiểm định màn chơi |
| Lập trình gameplay (Unity/Godot) | Cơ chế bắn, vật lý rơi khối, camera xoay |
| Hoạ sĩ 3D/voxel | Thiết kế bộ voxel, hiệu ứng particle, theme "Land" |
| UI/UX designer | Luồng màn hình, HUD, màn thắng/thua |
| Sound designer | Nhạc nền, SFX |
| LiveOps/Monetization | Cấu hình ads, IAP, cân bằng kinh tế coin/booster |

### Phạm vi MVP (đề xuất phát hành bản đầu tiên)
- 100–150 màn thủ công (đủ 3 giai đoạn: Giới thiệu → Làm quen → Trung bình)
- 1 loại khối đặc biệt (Locked block) để thử nghiệm
- 2 "Land" theme
- Hệ thống booster cơ bản (Hint, +1 shot)
- Interstitial + rewarded ads, IAP gỡ quảng cáo

### Giai đoạn 2 (sau ra mắt)
- Mở rộng khối đặc biệt (Multi-color, Bomb, Frozen)
- Thêm 3–4 Land theme mới
- Daily Puzzle, sự kiện giới hạn thời gian
- Bảng xếp hạng tuần (mang tính trang trí, không ganh đua khốc liệt)

---

## 12. Chỉ số thành công tham khảo (KPI ngành casual puzzle)

| Chỉ số | Mục tiêu tham khảo |
|---|---|
| D1 Retention | ~35–40% |
| D7 Retention | ~12–15% |
| D30 Retention | ~5–6% |
| Thời lượng phiên trung bình | 5–8 phút |
| Số phiên/ngày/người dùng | 3–5 |

*(Đây là benchmark chung của thể loại casual/puzzle mobile, cần đo đạc thực tế qua soft-launch để hiệu chỉnh.)*

---

## 13. Rủi ro & lưu ý

- **Thương hiệu:** "Cube Land" trùng tên hiển thị với sản phẩm đang phát hành của Rotatelab (*Cube Land Puzzle Game*) và cũng gần trùng với game khác "Cube Land Puzzle" (Yoshidahcc, Nhật). Nếu phát hành thương mại, nên cân nhắc đổi tên/định vị thương hiệu riêng để tránh nhầm lẫn hoặc tranh chấp, và không sao chép asset/UI 1:1 từ ứng dụng tham khảo – tài liệu này chỉ tham khảo **cơ chế gameplay dạng ý tưởng**, phần mỹ thuật/asset/mã nguồn cần được xây dựng độc lập.
- **Kỹ thuật:** cơ chế vật lý rơi khối + xoay 360° + nhiều lớp voxel ẩn có thể tốn hiệu năng trên máy cấu hình thấp – cần benchmark sớm.
- **Kinh tế game:** cân bằng coin/booster cần thử nghiệm A/B để tránh lặp lại "tường khó ép mua" như phản hồi tiêu cực ở game tham khảo.

---

*Tài liệu này được biên soạn dựa trên khảo sát trang Google Play/App Store, mô tả nhà phát triển và các bài đánh giá công khai của game "Cube Land Puzzle Game" (Rotatelab) tính đến 15/07/2026, kết hợp đề xuất thiết kế bổ sung để xây dựng một sản phẩm độc lập mang tên Cube Land.*
