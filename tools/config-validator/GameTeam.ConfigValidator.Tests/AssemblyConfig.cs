// SchemaSet đăng ký schema vào SchemaRegistry.Global (state toàn cục) → tắt chạy song song
// để tránh tranh chấp khi nhiều test cùng build SchemaSet. Bộ test nhỏ & nhanh nên không ảnh hưởng.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
