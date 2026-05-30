using System.Numerics;

namespace KingdomHeartsPlugin.UIElements.ParameterResource
{
    public static class StylizedText
    {
        public const float ResourceCharacterSpacing = 6.0f;
        
        public static readonly FlatMesh ResourceC = FlatMesh.Construct([
            // C, clockwise from top left
            // top bar: [0]WNW (left NW), [1]NNW, [2]NE,
            new Vector2(0.0f, 3.0f), new Vector2(4.0f, 0.0f), new Vector2(25.0f, 0.0f),
            // [3]SE, [4]SW (left NE)
            new Vector2(25.0f, 3.0f), new Vector2(7.0f, 3.0f),
            // bottom bar: [5]NW (left SE), [6]NE, [7]SE,
            new Vector2(7.0f, 16.0f), new Vector2(25.0f, 16.0f), new Vector2(25.0f, 19.0f),
            // [8]SSW, [9] WSW (left SW)
            new Vector2(4.0f, 19.0f), new Vector2(0.0f, 16.0f),
        ], [
            // C top bar: WNW-NNW-SW, NNW-NE-SW, NE-SW-SE 
            0, 1, 4, 1, 2, 4, 2, 4, 3,
            // C left bar: NW-NE-SW, NE-SW-SE
            0, 4, 9, 4, 9, 5,
            // C bottom bar: WSW-SSW-NW, SSW-NW-SE, NW-SE-NE
            9, 8, 5, 8, 5, 7, 5, 7, 6,
        ]);
        
        public static readonly FlatMesh ResourceG = FlatMesh.Construct([
            // G, clockwise from top left
            // top bar: [0]top WNW (left NW), [1]top NNW, [2]top NE, [3]top SE
            new Vector2(0.0f, 3.0f), new Vector2(4.0f, 0.0f), new Vector2(25.0f, 0.0f), new Vector2(25.0f, 3.0f),
            // left inside: [4]left NE, [5]left SE/bottom NW
            new Vector2(7.0f, 3.0f), new Vector2(7.0f, 16.0f),
            // right inside: [6]bottom NE/right SW, [7]right NW/lip SE, [8]lip SW 
            new Vector2(18.0f, 16.0f), new Vector2(18.0f, 13.0f), new Vector2(12.0f, 13.0f),
            // top of lip: [9]lip NW, [10]lip/right NE
            new Vector2(12.0f, 10.0f), new Vector2(25.0f, 10.0f), 
            // bottom bar: [11] right/bottom SE, [12]bottom SSW, [13]bottom WSW/left SW
            new Vector2(25.0f, 19.0f), new Vector2(4.0f, 19.0f), new Vector2(0.0f, 16.0f),
        ], [
            // G top bar: WNW-NNW-SW, NNW-NE-SW, NE-SW-SE 
            0, 1, 4, 1, 2, 4, 2, 4, 3,
            // G left bar: NW-NE-SW, NE-SW-SE
            0, 4, 13, 4, 13, 5,
            // G bottom bar: WSW-SSW-NW, SSW-NW-SE, NW-SE-NE
            13, 12, 5, 12, 5, 11, 5, 11, 6,
            // G right bar: NW-NE-SW, NE-SW-SE
            7, 10, 6, 10, 6, 11,
            // G lip: NW-NE-SW, NE-SW-SE
            9, 10, 8, 10, 8, 7,
        ]);

        public static readonly FlatMesh ResourceM = FlatMesh.Construct([
            // M, clockwise from top left
            // top bar: [0]NW, [1]NE ([2]SE is NE of right leg, [10]SW is NE of left leg)
            new Vector2(0.0f), new Vector2(30.0f, 0.0f),
            // right leg: [2]NE, [3]SE, [4]SW, [5]NW
            new Vector2(35.0f, 3.0f), new Vector2(35.0f, 19.0f), new Vector2(28.0f, 19.0f), new Vector2(28.0f, 3.0f),
            // center leg: [6]NE, [7]SE, [8]SW, [9]NW
            new Vector2(21.0f, 3.0f), new Vector2(21.0f, 19.0f), new Vector2(14.0f, 19.0f), new Vector2(14.0f, 3.0f),
            // left leg: [10]NE, [11]SE, [12]SW ([0]NW is NW of top bar)
            new Vector2(7.0f, 3.0f), new Vector2(7.0f, 19.0f), new Vector2(0.0f, 19.0f),
        ], [
            // M right leg: NE-SE-SW, NE-SW-NW
            2, 3, 4, 2, 4, 5,
            // M center leg: NE-SE-SW, NE-SW-NW
            6, 7, 8, 6, 8, 9,
            // M left leg: NE-SE-SW, NE-SW-NW
            10, 11, 12, 10, 12, 0,
            // M top bar: NE-SE-SW, NE-SW-NW
            1, 10, 2, 1, 10, 0,
        ]);

        public static readonly FlatMesh ResourceP = FlatMesh.Construct(
        [
            // P outside, clockwise from top left
            // top of loop: [0]outer NW, [1]outer NNE
            new Vector2(0.0f, 0.0f), new Vector2(19.0f, 0.0f),
            // right side of loop: [2]outer ENE, [3]outer ESE, [4]outer SSE
            new Vector2(23.0f, 4.0f), new Vector2(23.0f, 10.0f), new Vector2(19.0f, 13.0f),
            // stem: [5]outer NE, [6]outer SE, [7]outer SW ([0]outer NW is first point)
            new Vector2(6.0f, 13.0f), new Vector2(6.0f, 19.0f), new Vector2(0.0f, 19.0f),

            // P inside, counter-clockwise from top left
            // [8]inner NW, [9]inner SW, [10]inner SE, [11]inner NE,
            new Vector2(6.0f, 3.0f), new Vector2(6.0f, 10.0f), new Vector2(17.0f, 10.0f), new Vector2(17.0f, 3.0f),
        ], [
            // P loop top bar: oNW-oNNE-iNE, oNW-iNW-iNE
            0, 1, 11, 0, 8, 11,
            // P loop top-right corner: oNNE-oENE-iNE
            1, 2, 11,
            // P loop right bar: iSE-iNE-oENE, iSE-oENE-oESE
            10, 11, 2, 10, 2, 3,
            // P loop bottom-right corner: iSE-oESE-oSSE
            10, 3, 4,
            // P loop bottom bar: iSW-iSE-oNE, oNE-iSE-oSSE
            9, 10, 5, 5, 10, 4,
            // P left side: iNW-oSE-oSW, oSE-oSW-oNW
            8, 7, 6, 8, 7, 0,
        ], [
            8, // P outside
            4, // P inside
        ]);
    }
}