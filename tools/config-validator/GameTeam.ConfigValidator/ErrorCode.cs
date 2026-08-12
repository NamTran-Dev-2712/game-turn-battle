namespace GameTeam.ConfigValidator;

/// <summary>
/// Mã lỗi ổn định cho báo cáo validate (tài liệu: tools/config-validator/README.md §Error codes).
/// Đặt tên theo NHÓM để công cụ CI/agent tương lai phân loại được; giá trị chuỗi là hợp đồng ổn định.
/// </summary>
public enum ErrorCode
{
    /// <summary>JSON001 — file không phải JSON hợp lệ (không parse được).</summary>
    Json001Parse,

    /// <summary>MAP001 — file config nằm ở thư mục không map được sang schema (loại không xác định).</summary>
    Map001UnknownType,

    /// <summary>SCH001 — vi phạm JSON Schema (kèm keyword + instance path).</summary>
    Sch001Schema,

    /// <summary>VER001 — thiếu schema_version hoặc không phải số nguyên.</summary>
    Ver001MissingOrInvalid,

    /// <summary>VER002 — schema_version không được hỗ trợ (khác phiên bản hiện tại).</summary>
    Ver002Unsupported,

    /// <summary>REF001 — id được tham chiếu không tồn tại trong index (thiếu tham chiếu).</summary>
    Ref001Missing,

    /// <summary>REF002 — tham chiếu sai định dạng / sai loại đích (vd currency ref không hợp lệ).</summary>
    Ref002Invalid,
}

/// <summary>Ánh xạ <see cref="ErrorCode"/> sang chuỗi mã ổn định dùng trong report (file:path:CODE).</summary>
public static class ErrorCodes
{
    public static string ToToken(this ErrorCode code) => code switch
    {
        ErrorCode.Json001Parse => "JSON001",
        ErrorCode.Map001UnknownType => "MAP001",
        ErrorCode.Sch001Schema => "SCH001",
        ErrorCode.Ver001MissingOrInvalid => "VER001",
        ErrorCode.Ver002Unsupported => "VER002",
        ErrorCode.Ref001Missing => "REF001",
        ErrorCode.Ref002Invalid => "REF002",
        _ => code.ToString(),
    };
}
