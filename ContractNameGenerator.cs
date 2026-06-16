using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrashoutCrew_APClient;

public static class ContractNameGenerator
{
    private static Dictionary<string, string[]> _orderToAdjectives = new()
    {
        { "order-anvil", ["Heavy", "Hard"] },
        { "order-barreloil", ["Gloopy", "Sticky"] },
        { "order-barrelooze", ["Corrosive"] },
        { "order-battery", ["Speedy", "Powerful"] },
        { "order-beehive", ["Buzzy", "Stingy"] },
        { "order-bingbong", ["Bingy", "Bongy"] },
        { "order-blueberry", ["Blueish", "Tasty", "Fruity", "Squishy"] },
        { "order-bomb", ["Dangerous", "Bangy"] },
        { "order-bull", ["Bullish", "Horny"] },
        { "order-cherry", ["Red", "Tasty", "Fruity", "Squishy"] },
        { "order-chicken", ["Feathery", "BKAWK"] },
        { "order-clownCar", ["Fast", "Furious"] },
        { "order-cubeToy", ["Colorful", "Cubic"] },
        { "order-egg", ["Eggy", "Cracked"] },
        { "order-fireworks", ["Pretty", "Sparkly"] },
        { "order-ghostBox", ["Haunted", "Spooky", "Ghostly", "Scary"] },
        { "order-grape", ["Purple", "Grapey", "Squishy", "Fruity", "Tasty"] },
        { "order-honeyPot", ["Sweet", "Sticky"] },
        { "order-kiwi", ["Green", "Fuzzy", "Squishy", "Fruity", "Tasty"] },
        { "order-lantern", ["Bright", "Hot", "Fiery"] },
        { "order-monkey", ["Cheeky", "Sneaky"] },
        { "order-snail", ["Slimy", "Slow", "Shelly"] },
        { "order-vase", ["Fragile", "Expensive"] }
    };

    private static Dictionary<string, string[]> _orderToVerb = new()
    {
        { "order-anvil", ["Falling", "Crushing"] },
        { "order-barreloil", ["Spilling", "Oozing"] },
        { "order-barrelooze", ["Burning", "Oozing"] },
        { "order-battery", ["Energizing", "Powering"] },
        { "order-beehive", ["Buzzing"]},
        { "order-bingbong", ["Crying", "Squeaking"] },
        { "order-blueberry", [] },
        { "order-bomb", ["Exploding", "Detonating"] },
        { "order-bull", ["Charging", "Bucking", "Bullying"] },
        { "order-cherry", [] },
        { "order-chicken", ["Clucking", "Laying"] },
        { "order-clownCar", ["Honking", "Driving"] },
        { "order-cubeToy", ["Solving", "Scrambling"] },
        { "order-egg", ["Cracking", "Frying"] },
        { "order-fireworks", ["Exploding", "Flying", "Whistling"] },
        { "order-ghostBox", ["Spooking", "Scaring"] },
        { "order-grape", [] },
        { "order-honeyPot", ["Sticking"] },
        { "order-kiwi", [] },
        { "order-lantern", ["Lighting", "Flaming", "Glowing"] },
        { "order-monkey", ["Swinging", "Throwing"] },
        { "order-snail", ["Crawling"] },
        { "order-vase", ["Smashing", "Breaking"] }
    };

    private static Dictionary<string, string[]> _orderToNoun = new()
    {
        { "order-anvil", ["Anvils", "Weights", "Metal"] },
        { "order-barreloil", ["Oil", "Barrels", "Mess"] },
        { "order-barrelooze", ["Acid", "Barrels", "Waste"] },
        { "order-battery", ["Batteries", "Energy", "Power"] },
        { "order-beehive", ["Bees", "Hives", "Buzzers"] },
        { "order-bingbong", ["BingBong", "Husband"] },
        { "order-blueberry", ["Blueberries", "Fruit"] },
        { "order-bomb", ["Bombs", "Explosives", "Destruction"] },
        { "order-bull", ["Bulls", "Animals", "Horns"] },
        { "order-cherry", ["Cherries", "Fruit"] },
        { "order-chicken", ["Chickens", "Cluckers", "Feathers"] },
        { "order-clownCar", ["Cars", "Drivers", "Wheels"] },
        { "order-cubeToy", ["Cubes", "Puzzles", "Solvers"] },
        { "order-egg", ["Eggs", "Shells"] },
        { "order-fireworks", ["Fireworks", "Explosives", "Lights"] },
        { "order-ghostBox", ["Ghosts", "Ghouls", "Haunt"] },
        { "order-grape", ["Grapes", "Fruit"] },
        { "order-honeyPot", ["Honey", "Pots"] },
        { "order-kiwi", ["Kiwis", "Fruit"] },
        { "order-lantern", ["Lanterns", "Fire", "Lights"] },
        { "order-monkey", ["Monkeys", "Bananas", "Peels"] },
        { "order-snail", ["Snails", "Shells"] },
        { "order-vase", ["Vases", "Pots"] }
    };

    public static string GenerateName(this ShiftOrderObject[] orders, Random random)
    {
        var words = new List<string?>();
        for (var orderIndex = 0; orderIndex < orders.Length; orderIndex++)
        {
            if (orderIndex == orders.Length - 1)
            {
                words.Add(_orderToNoun[orders[orderIndex].name].Random(random: random));
                break;
            }
            if (orderIndex == orders.Length - 2 && _orderToVerb[orders[orderIndex].name].Length != 0)
                words.Add(_orderToVerb[orders[orderIndex].name].Random(random: random));
            else
                words.Add(_orderToAdjectives[orders[orderIndex].name].Random(random: random));
        }
        return string.Join(" ", words);
    }
}