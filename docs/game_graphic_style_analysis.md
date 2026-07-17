# GAME GRAPHIC STYLE ANALYSIS  
## Visual Direction Document — Voxel Destruction Puzzle

> Góc nhìn: **Game Graphic Designer / Art Director**  
> Mục tiêu: Phân tích hình ảnh tham chiếu và chuyển hóa thành định hướng đồ họa có thể áp dụng cho quá trình dựng lại game.

---

# 1. Định vị phong cách hình ảnh

Phong cách tổng thể là sự kết hợp giữa:

- **Stylized 3D**
- **Voxel / block-based geometry**
- **Plastic toy material**
- **Hyper-casual puzzle presentation**
- **Satisfying destruction VFX**

Hình ảnh không đi theo hướng voxel thô như Minecraft, mà được xử lý theo phong cách đồ chơi 3D hiện đại:

- Khối có bevel.
- Bề mặt bóng mềm.
- Màu sắc bão hòa cao.
- Đổ bóng sạch và dễ đọc.
- Hiệu ứng phá hủy nhiều chi tiết nhưng vẫn kiểm soát tốt bố cục.

Từ khóa art direction:

`Chunky` · `Toy-like` · `Colorful` · `Clean` · `Juicy` · `Satisfying` · `Readable`

---

# 2. Mục tiêu cảm xúc

Visual cần tạo ra ba cảm giác chính:

## 2.1. Satisfying

Người chơi phải cảm nhận rõ:

- Khối bị phá vỡ.
- Các mảnh bị bật ra.
- Lớp bên ngoài bị bóc dần.
- Phần lõi màu xanh dần được lộ ra.

## 2.2. Toy-like

Mọi object nên giống:

- Đồ chơi nhựa.
- Gạch lắp ráp.
- Kẹo cứng.
- Khối puzzle mềm mại và thân thiện.

## 2.3. Clear and readable

Dù hiệu ứng phá hủy phức tạp, người chơi vẫn phải đọc được:

- Mục tiêu chính.
- Màu nào đang được xử lý.
- Block nào còn lại.
- Khu vực nào đang bị phá.
- Số lượng hoặc trạng thái của các item phía dưới.

---

# 3. Shape Language

## 3.1. Ngôn ngữ hình khối chính

Toàn bộ game sử dụng ngôn ngữ hình học:

- Vuông.
- Chữ nhật.
- Khối hộp.
- Góc bo nhẹ.
- Tỷ lệ dày và chắc.

Không nên sử dụng shape quá mỏng, sắc hoặc realistic.

### Đặc điểm hình học đề xuất

- Cube có bevel nhẹ.
- Bevel chiếm khoảng `5–10%` cạnh.
- Mỗi block có silhouette rõ.
- Block không hoàn toàn bằng phẳng.
- Khoảng cách giữa các block đủ nhỏ để tạo cảm giác liền khối.
- Khe giữa block được nhấn bằng ambient occlusion.

---

## 3.2. Main Container

Khối container chính là focal point của toàn màn hình.

Đặc điểm:

- Dạng hộp chữ nhật lớn.
- Xoay góc 3/4.
- Chiếm khoảng `40–50%` chiều cao màn hình.
- Thành ngoài được cấu thành từ nhiều block nhỏ.
- Pattern đỏ và cam tạo cảm giác checkerboard.
- Mặt trước đang bị phá để lộ lõi xanh bên trong.
- Thành hộp dày, giúp tăng cảm giác có thể tích.

Container cần có cảm giác:

- Nặng.
- Dày.
- Chứa nhiều block bên trong.
- Có cấu trúc nhiều lớp.

---

## 3.3. Inner Core

Lõi bên trong sử dụng màu xanh lá để tạo tương phản mạnh với lớp đỏ.

Vai trò thị giác:

- Là vùng mục tiêu.
- Là phần cần được giải phóng.
- Tạo điểm nghỉ mắt giữa vùng phá hủy hỗn loạn.
- Giữ cho người chơi luôn hiểu trạng thái gameplay.

Shape nên:

- Gọn.
- Liên kết thành cụm rõ.
- Ít nhiễu hơn vùng mảnh vỡ.
- Có màu đồng nhất hơn lớp ngoài.

---

## 3.4. Debris Shapes

Mảnh vỡ nên chia thành ba cấp độ:

### Large debris

- Kích thước gần bằng block tiêu chuẩn.
- Dùng để truyền tải lực phá mạnh.
- Có rotation rõ.
- Bay ra theo nhiều hướng.

### Medium debris

- Kích thước khoảng `50–70%` block tiêu chuẩn.
- Tạo cảm giác mật độ.
- Chuyển động nhanh hơn large debris.

### Small debris

- Kích thước khoảng `20–40%`.
- Tạo cảm giác vụn vỡ.
- Có thể kết hợp trail.
- Không nên quá nhiều đến mức che gameplay.

---

# 4. Material Direction

## 4.1. Solid Block Material

Vật liệu khối đặc là dạng:

**Stylized plastic / soft PBR**

Không phải kim loại, không phải cao su, không phải đá.

### Đặc điểm

- Highlight rộng.
- Phản sáng vừa phải.
- Diffuse mềm.
- Không quá bóng như kính.
- Không có texture bề mặt phức tạp.
- Giá trị màu chủ yếu đến từ shader và lighting.

### Thông số tham khảo

```text
Metallic:      0
Smoothness:    0.45–0.65
Specular:      0.35–0.55
Normal:        Dựa chủ yếu vào bevel geometry
Emission:      0 hoặc rất thấp
Occlusion:     Mạnh tại các khe
```

---

## 4.2. Transparent Block Material

Các khối trong suốt có phong cách:

**Stylized acrylic / candy glass**

Không nên dùng glass shader realistic.

### Đặc điểm

- Alpha vừa phải.
- Viền sáng rõ.
- Fresnel nhẹ.
- Highlight mạnh hơn block đặc.
- Không dùng refraction mạnh.
- Giữ được silhouette khi nhiều object chồng nhau.

### Thông số tham khảo

```text
Surface:       Transparent
Blend:         Alpha hoặc Premultiply
Alpha:         0.25–0.55
Smoothness:    0.70–0.90
Metallic:      0
Fresnel Power: 2–4
Specular:      0.70–1.00
```

### Lưu ý thiết kế

Transparent block phải:

- Nhìn xuyên được.
- Nhưng vẫn giữ được cạnh.
- Không hòa hoàn toàn vào background.
- Không gây sorting artifact quá rõ.

Có thể tăng alpha hoặc highlight ở cạnh để cải thiện readability.

---

# 5. Shader Style

## 5.1. Tổng thể

Shader nên nằm giữa:

- PBR mềm.
- Toon shading nhẹ.
- Stylized color grading.

Không nên sử dụng cel-shading cứng với 2–3 band quá rõ.

---

## 5.2. Face Color Variation

Mỗi mặt cube không nên có màu giống hệt nhau.

Gợi ý:

- Mặt hướng sáng: tăng brightness.
- Mặt bên: giảm brightness khoảng `10–15%`.
- Mặt khuất: chuyển nhẹ sang màu lạnh hoặc đỏ tím.
- Không chỉ đơn giản nhân màu đen.

Ví dụ block đỏ:

- Light face: đỏ cam.
- Mid face: đỏ chính.
- Shadow face: đỏ tím hoặc đỏ nâu.

Điều này giúp cube đọc rõ thể tích ngay cả khi object nhỏ trên màn hình.

---

## 5.3. Per-Block Color Variation

Nên thêm variation nhẹ giữa các block:

```text
Hue variation:        ±2–4%
Brightness variation: ±3–6%
Saturation variation: ±2–5%
```

Mục đích:

- Tránh cảm giác copy-paste.
- Tạo độ sống cho khối.
- Giữ pattern nhưng không quá phẳng.

Variation không nên quá mạnh vì sẽ làm nhiễu màu gameplay.

---

# 6. Color Direction

## 6.1. Bảng màu chính

| Vai trò | Màu tham khảo |
|---|---|
| Background navy | `#263B65` |
| Background dark | `#1F2E4B` |
| Deep red | `#A92318` |
| Main red | `#C93620` |
| Red-orange | `#E16333` |
| Orange highlight | `#FF7543` |
| Deep green | `#128B4D` |
| Main green | `#32C76A` |
| Light green | `#68DB89` |
| Purple accent | `#6A1DDB` |
| Yellow accent | `#F4C21C` |
| Warm white | `#FFF5E8` |
| Dark outline | `#302128` |

---

## 6.2. Phân cấp màu

### Background

- Dùng xanh navy.
- Ít chi tiết.
- Không gradient quá mạnh.
- Tạo nền tối để object nổi bật.

### Outer container

- Đỏ và cam.
- Saturation cao.
- Tạo cảm giác nóng, mạnh và dễ chú ý.

### Inner core

- Xanh lá.
- Tương phản trực tiếp với đỏ.
- Trở thành focal gameplay.

### Accent

- Vàng dùng cho coin, CTA và thông tin quan trọng.
- Tím dùng cho banner hoặc điểm nhấn phụ.
- Trắng dùng cho số và label.

---

## 6.3. Nguyên tắc tương phản

Visual nên giữ ba mức tương phản:

1. **Main object vs background**
2. **Outer shell vs inner core**
3. **UI text vs UI block**

Không nên để quá nhiều màu cùng tranh nhau làm focal point.

---

# 7. Lighting Direction

## 7.1. Key Light

Nguồn sáng chính:

- Chiếu từ trên-trái.
- Góc cao.
- Màu trắng hơi ấm.
- Cường độ trung bình đến cao.
- Shadow mềm.

Mục tiêu:

- Làm sáng mặt trên.
- Tạo gradient rõ trên mặt bên.
- Tăng độ nổi khối.

---

## 7.2. Fill Light

Fill light nên:

- Rất nhẹ.
- Màu xanh lạnh.
- Giúp mặt tối không biến thành đen.
- Hòa cùng background navy.

---

## 7.3. Rim / Edge Highlight

Có thể thêm rim nhẹ ở:

- Mảnh trong suốt.
- Cạnh block hướng camera.
- Vùng phá hủy.
- Object cần nhấn mạnh.

Rim không nên xuất hiện đồng đều trên toàn bộ object.

---

## 7.4. Shadow

Shadow cần:

- Mềm.
- Có màu xanh đen.
- Không quá đậm.
- Không tạo cảm giác realistic nặng nề.

Ambient occlusion quan trọng hơn cast shadow trong việc tách các block nhỏ.

---

# 8. Destruction VFX

## 8.1. Mục tiêu thị giác

Destruction VFX phải tạo cảm giác:

- Có lực.
- Có khối lượng.
- Nhiều lớp.
- Nhanh nhưng vẫn đọc được.
- Không phá vỡ focal point.

---

## 8.2. Layer Structure

Hiệu ứng phá hủy nên có các lớp:

### Layer A — Main chunks

Các block lớn bay ra ngoài.

### Layer B — Secondary fragments

Các block nhỏ tạo mật độ.

### Layer C — Transparent shards

Tạo sparkle và cảm giác vật liệu vỡ.

### Layer D — Motion trails

Nhấn hướng chuyển động và tốc độ.

### Layer E — Micro particles

Dùng rất ít để tăng độ “juicy”.

---

## 8.3. Motion Design

Chuyển động đề xuất:

- Burst nhanh trong `0.10–0.25s`.
- Rotation ngẫu nhiên cả 3 trục.
- Vận tốc ban đầu hướng ra ngoài.
- Sau burst, gravity kéo xuống.
- Một số block va chạm và bật nhẹ.
- Không để mọi mảnh bay cùng tốc độ.

### Phân bố vận tốc

- Large debris: chậm, nặng.
- Medium debris: nhanh vừa.
- Small debris: nhanh nhất.
- Transparent shard: tốc độ cao nhưng lifetime ngắn.

---

## 8.4. Trail Style

Trail nên:

- Thẳng.
- Mảnh.
- Có cảm giác hình học.
- Fade nhanh.
- Không giống khói hoặc lửa.

Thông số tham khảo:

```text
Blend:        Alpha Blend
Start Alpha:  0.40–0.70
End Alpha:    0
Width:        Mảnh
Lifetime:     0.15–0.35s
Emission:     Rất nhẹ
```

---

# 9. Camera & Composition

## 9.1. Camera Angle

Camera sử dụng góc:

- 3/4 view.
- Gần isometric.
- Có perspective nhẹ.
- Nhìn từ trên xuống.

Gợi ý:

```text
Yaw:   35–45°
Pitch: 25–35°
FOV:   25–40°
```

FOV không nên quá rộng vì sẽ làm méo container.

---

## 9.2. Composition

Main object cần:

- Nằm gần trung tâm.
- Hơi lệch lên trên.
- Để khoảng trống bên dưới cho gameplay UI.
- Không chạm sát mép màn hình.
- Có đủ negative space để mảnh vỡ bay ra.

---

## 9.3. Focal Hierarchy

Thứ tự chú ý:

1. Vùng container đang bị phá.
2. Lõi xanh bên trong.
3. Các block số hoặc item điều khiển.
4. Level title.
5. Currency và setting.

Không để coin hoặc button setting quá nổi hơn gameplay.

---

# 10. UI Graphic Direction

## 10.1. General UI Style

UI sử dụng phong cách:

- Chunky.
- Rounded.
- Toy-like.
- High contrast.
- Ít chi tiết.
- Dễ đọc trên màn hình nhỏ.

---

## 10.2. Number Blocks

Các block số phía dưới có:

- Hình hộp đứng.
- Góc bo lớn.
- Mặt trước sáng.
- Cạnh dưới tối.
- Shadow xanh đen.
- Text trắng có outline.

Cảm giác nên giống:

- Kẹo.
- Viên pin đồ chơi.
- Piece trong board game.

---

## 10.3. Typography

Font style:

- Sans-serif.
- Extra bold.
- Hơi condensed.
- Viết hoa.
- Bo mềm.
- Outline dày.
- Drop shadow rõ.

### Text hierarchy

- Level: trắng, cỡ trung bình.
- Số lượng: trắng hoặc warm white.
- CTA: vàng hoặc trắng.
- Outline: nâu đen hoặc navy đậm.

---

## 10.4. Button Style

Button setting:

- Rounded square.
- Gradient xanh.
- Viền xanh đậm.
- Icon trắng.
- Shadow nhẹ.
- Kích thước lớn đủ để đọc trên mobile.

---

# 11. Post-Processing

Post-processing nên được sử dụng có kiểm soát.

## Đề xuất

- Saturation: tăng nhẹ.
- Contrast: tăng vừa.
- Bloom: rất nhẹ.
- Ambient Occlusion: khá rõ.
- Vignette: nhẹ.
- Anti-aliasing: bắt buộc.
- Sharpen: thấp hoặc không dùng.
- Depth of Field: rất nhẹ hoặc tắt.

## Không nên

- Bloom toàn màn hình.
- Chromatic aberration mạnh.
- Motion blur nặng.
- Film grain.
- Color grading quá cinematic.
- Shadow quá realistic.

---

# 12. Quy chuẩn asset

## 12.1. Block tiêu chuẩn

Mỗi block nên có:

- Một mesh cube bevel dùng chung.
- Material instance hoặc GPU instancing.
- Có vertex color hoặc color parameter.
- Pivot ở center.
- Scale đồng nhất.
- UV đơn giản.

---

## 12.2. Bevel

Bevel là yếu tố bắt buộc để đạt đúng style.

Không bevel:

- Block nhìn rẻ.
- Highlight không đẹp.
- Silhouette quá sắc.
- Thiếu cảm giác đồ chơi.

Bevel quá lớn:

- Block giống viên kẹo.
- Mất chất voxel.
- Pattern không còn rõ.

Tỷ lệ đề xuất:

```text
Bevel width: 5–10% kích thước cạnh
Bevel segments: 1–2
```

---

## 12.3. Texture

Nên ưu tiên shader màu phẳng thay vì texture chi tiết.

Có thể dùng:

- Gradient map nhỏ.
- Mask đơn giản.
- Noise rất nhẹ.
- Vertex color.

Không nên dùng:

- Scratch texture.
- Dirt.
- Grunge.
- Surface detail realistic.
- Normal map quá rõ.

---

# 13. Visual Do & Don’t

## Do

- Dùng block bevel.
- Dùng màu bão hòa cao.
- Giữ background sạch.
- Nhấn AO giữa các block.
- Sử dụng highlight rộng.
- Phân lớp debris rõ ràng.
- Giữ lõi mục tiêu dễ đọc.
- Dùng silhouette lớn và đơn giản.
- Ưu tiên cảm giác đồ chơi.

## Don’t

- Không dùng cube cạnh sắc hoàn toàn.
- Không làm vật liệu kim loại.
- Không dùng texture realistic.
- Không dùng glass refraction mạnh.
- Không để VFX che hết gameplay.
- Không dùng quá nhiều màu accent.
- Không để shadow chuyển thành đen.
- Không dùng motion blur nặng.
- Không làm camera perspective quá rộng.

---

# 14. Art Production Formula

Công thức ngắn gọn để tái tạo style:

```text
Geometry:
Beveled cube + chunky proportion + dense block pattern

Material:
Stylized plastic + soft specular + zero metallic

Color:
High saturation + strong red/green contrast + dark navy background

Lighting:
Warm top-left key light + cool ambient fill + strong AO

VFX:
Layered cube debris + transparent shards + short geometric trails

Camera:
3/4 top-down + low perspective distortion

UI:
Rounded chunky panels + bold outlined typography + toy-like icons
```

---

# 15. Kết luận Art Direction

Cốt lõi phong cách không nằm ở việc “dùng nhiều cube”, mà nằm ở cách các cube được xử lý như một hệ thống đồ họa thống nhất:

- Shape vuông nhưng không cứng.
- Shader bóng nhưng không realistic.
- Màu rực nhưng có phân cấp.
- VFX nhiều nhưng vẫn đọc được.
- UI đơn giản nhưng có chiều sâu.
- Chuyển động nhanh nhưng giữ được trọng lượng.

Kết quả cuối cùng cần tạo cảm giác như một món đồ chơi 3D đang bị phá vỡ theo cách vui mắt, rõ ràng và thỏa mãn.
