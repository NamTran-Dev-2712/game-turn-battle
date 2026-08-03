# Hướng dẫn phát triển (Development Guide)

> Cách làm việc hằng ngày trong repo. Chuẩn chi tiết ở [`docs/conventions/`](docs/conventions/).

## Chuẩn bị
Xem [SETUP.md](SETUP.md) (cài .NET 9, Godot 4.x, Docker).

## Vòng lặp phát triển
1. Nhận task → nạp ngữ cảnh ([AI_GUIDE.md](AI_GUIDE.md) / [docs/ai/context-strategy.md](docs/ai/context-strategy.md)).
2. Nhánh từ `dev`: `feature/<id>-<slug>`.
3. Code theo [STYLE_GUIDE.md](STYLE_GUIDE.md); giữ PR nhỏ.
4. Viết/chạy test cục bộ:
   - Backend: `dotnet test server/GameTeam.sln`
   - Client: chạy gdUnit4 trong Godot / headless (khi CI sẵn sàng)
5. Commit **Conventional Commits**; mở PR (điền DoD).
6. CI xanh + review CODEOWNERS → squash-merge.

## Ranh giới cần nhớ
- Backend: Domain thuần; Application không phụ thuộc Infrastructure (kiểm bằng NetArchTest).
- Client: feature không import chéo (dùng EventBus/signals).
- Cân bằng gameplay ở `config/`, không hardcode.
- Quyết định nhạy cảm ở server.

## Cấu trúc dự án
| Phần | Chi tiết |
|---|---|
| Backend | [server/README.md](server/README.md), [docs/backend](docs/backend/) |
| Client | [client/README.md](client/README.md), [docs/godot](docs/godot/) |
| Config | [config/README.md](config/README.md), [docs/adr/ADR-005](docs/adr/) |

## Khi bí
[SUPPORT.md](SUPPORT.md). Điểm chưa chốt → [docs/mvp/10-open-questions.md](docs/mvp/10-open-questions.md).
