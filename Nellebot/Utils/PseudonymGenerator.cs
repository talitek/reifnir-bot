using ZooIds;

namespace Nellebot.Utils;

public class PseudonymGenerator
{
    public static readonly GeneratorConfig ZooIdsConfig = new(1, " ", CaseStyle.TitleCase);

    public static string GetRandomPseudonym()
    {
        var zoo = new ZooIdGenerator(ZooIdsConfig);

        return zoo.GenerateId();
    }
}
