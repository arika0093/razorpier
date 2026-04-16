using Razorpier.Core;

var failures = new List<string>();

ShouldFormatTopLevelSections(failures);
ShouldIndentMarkupBlocks(failures);

if (failures.Count == 0)
{
    Console.WriteLine("All Razorpier formatter tests passed.");
    return 0;
}

foreach (var failure in failures)
{
    Console.Error.WriteLine(failure);
}

return 1;

static void ShouldFormatTopLevelSections(List<string> failures)
{
    const string input =
        """
        @inject WeatherForecastService Service
        <div>
        <p>Hello</p>
        </div>
        @using Zebra
        @code {
        private int count=0;
        void Increment(){
        count++;
        }
        }
        @page "/counter"
        @using Alpha
        """;

    const string expected =
        """
        @using Alpha

        @using Zebra

        @page "/counter"

        <div>
            <p>Hello</p>
        </div>

        @inject WeatherForecastService Service

        @code
        {
            private int count = 0;

            void Increment()
            {
                count++;
            }
        }
        """;

    AssertEqual("ShouldFormatTopLevelSections", expected + "\n", RazorFormatter.Format(input), failures);
}

static void ShouldIndentMarkupBlocks(List<string> failures)
{
    const string input =
        """
        <div>
        @if (true)
        {
        <span>Hello</span>
        }
        </div>
        """;

    const string expected =
        """
        <div>
            @if (true)
            {
                <span>Hello</span>
            }
        </div>
        """;

    AssertEqual("ShouldIndentMarkupBlocks", expected + "\n", RazorFormatter.Format(input), failures);
}

static void AssertEqual(string name, string expected, string actual, List<string> failures)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
    {
        failures.Add(
            $"""
            [FAIL] {name}
            Expected:
            ---
            {expected}
            ---
            Actual:
            ---
            {actual}
            ---
            """);
    }
}
