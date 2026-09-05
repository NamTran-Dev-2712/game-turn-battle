using GameTeam.CombatBaseline;

// combat-baseline: sinh/kiem golden-vector baseline tu sim server (nguon chan ly, ADR-011).
//   combat-baseline generate [file...]  -> ghi expected baseline (mac dinh: tat ca vector)
//   combat-baseline check    [file...]  -> so baseline tren dia voi sim server (khong ghi)
// Exit: 0 ok | 1 drift (chi check) | 2 loi tool/usage.
const int ExitOk = 0;
const int ExitDrift = 1;
const int ExitToolError = 2;

if (args.Length == 0)
{
    return Usage("thieu lenh");
}

string command = args[0];
string[] files = args.Skip(1).ToArray();

try
{
    BaselineTool tool = BaselineTool.ForRepo();
    switch (command)
    {
        case "generate":
        {
            IReadOnlyList<VectorOutcome> results = tool.Generate(files);
            foreach (VectorOutcome r in results)
            {
                Console.WriteLine($"{(r.Status == VectorStatus.Written ? "WRITE" : "OK   ")} {r.FileName}");
            }

            int written = results.Count(r => r.Status == VectorStatus.Written);
            Console.WriteLine($"generate: {results.Count} vector, {written} ghi lai, {results.Count - written} khong doi.");
            return ExitOk;
        }

        case "check":
        {
            IReadOnlyList<VectorOutcome> results = tool.Check(files);
            foreach (VectorOutcome r in results)
            {
                Console.WriteLine($"{(r.Status == VectorStatus.Drift ? "DRIFT" : "OK   ")} {r.FileName}");
            }

            int drift = results.Count(r => r.Status == VectorStatus.Drift);
            if (drift > 0)
            {
                Console.Error.WriteLine(
                    $"check: {drift}/{results.Count} vector LECH sim server. Chay 'combat-baseline generate' co chu dich " +
                    "(review diff + ghi ly do) — KHONG sua vector am tham. Xem tools/combat-baseline/README.md.");
                return ExitDrift;
            }

            Console.WriteLine($"check: {results.Count} vector khop sim server.");
            return ExitOk;
        }

        default:
            return Usage($"lenh khong ro: '{command}'");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"combat-baseline: loi tool — {ex.Message}");
    return ExitToolError;
}

int Usage(string reason)
{
    Console.Error.WriteLine($"combat-baseline: {reason}");
    Console.Error.WriteLine("dung: combat-baseline <generate|check> [file.json ...]");
    return ExitToolError;
}
