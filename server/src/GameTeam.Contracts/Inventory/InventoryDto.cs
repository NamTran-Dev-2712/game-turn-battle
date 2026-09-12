using GameTeam.Contracts.Hero;

namespace GameTeam.Contracts.Inventory;

/// <summary>
/// Kho đồ của người chơi (bản wire chỉ-đọc, server-authoritative — ADR-007). Là "sổ tài sản" thứ hai bên cạnh
/// ví (<c>WalletDto</c>). Gồm các chồng vật phẩm/mảnh (<see cref="Items"/>) + <b>chiếu</b> hero sở hữu
/// (<see cref="OwnedHeroes"/>, aggregate Phase 27 — không nhân bản). Client hiển thị từ đây (qua
/// <c>StateCache</c>), KHÔNG tự tính số lượng. Danh sách (thay cho map — codegen client chỉ hỗ trợ mảng).
/// </summary>
/// <param name="Items">Các chồng tài sản (vật phẩm/mảnh) — đã lọc/phân trang theo yêu cầu.</param>
/// <param name="OwnedHeroes">Hero người chơi sở hữu (chiếu từ aggregate OwnedHero — Phase 27).</param>
public sealed record InventoryDto(
    IReadOnlyList<ItemStackDto> Items,
    IReadOnlyList<OwnedHeroDto> OwnedHeroes);
