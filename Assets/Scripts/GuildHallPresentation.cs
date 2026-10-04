using System;
using System.Collections.Generic;
using UnityEngine;

// All positions are authored in the original 1672 x 941 room image.
// A seat owns the hip position, facing and depth; a guest owns their artwork.
public sealed class GuildHallPresentation : MonoBehaviour
{
    public const int GuestCount = 6;
    public const float Width = 1672f, Height = 941f, Ppu = 100f;
    public struct Seat
    {
        public Vector2 hip;
        public bool faceLeft;
        public float scale;
        public int order;
        public Seat(float x, float y, bool left, float size, int depth)
        { hip = new Vector2(x, y); faceLeft = left; scale = size; order = depth; }
    }
    // Furniture is grouped by seat index: left table (0-1), middle (2-3), right (4-5).
    public static readonly Seat[] Seats = {
        new Seat(187, 647, false, .54f, 10), new Seat(476, 641, true, .54f, 10),
        new Seat(680, 763, false, .60f, 30), new Seat(1093, 766, true, .60f, 30),
        new Seat(1230, 626, false, .52f, 10), new Seat(1540, 623, true, .52f, 10),
        new Seat(938, 472, true, .43f, -90)
    };
    // Hip positions measured within each full 512 x 512 character cell, top-left origin.
    private static readonly Vector2[] Hips = {
        new Vector2(268,325), new Vector2(255,323), new Vector2(260,325),
        new Vector2(270,292), new Vector2(268,307), new Vector2(278,295)
    };
    private static readonly bool[] SourceFacesLeft = { false, true, false, true, false, true };
    public Texture2D[] Portraits { get; private set; }
    public int[] SeatForGuest { get; private set; }
    public SpriteRenderer[] Renderers { get; private set; }
    private GuestSpriteMotion[] motions;
    private int[] artwork;
    private Sprite[] guestSprites;
    private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    private GameObject root;
    private Camera roomCamera;
    private SpriteRenderer background;
    private Sprite previousBackground;
    private Vector3 previousScale;
    private bool ready;
    private GuestSpriteMotion guildmasterMotion;
    private MeshRenderer guildmasterRenderer;
    private static readonly Vector2 GuildmasterAnchor = new Vector2(850, 402);
    private static readonly Vector2[] GuildmasterOutline = {
        new Vector2(835,307), new Vector2(853,307), new Vector2(866,315),
        new Vector2(870,331), new Vector2(867,347), new Vector2(872,358),
        new Vector2(880,371), new Vector2(887,400), new Vector2(813,400),
        new Vector2(812,385), new Vector2(816,369), new Vector2(823,352),
        new Vector2(819,337), new Vector2(823,319)
    };
    public bool Ready => ready;

    public void Initialize(Camera camera)
    {
        if (ready) return;
        roomCamera = camera;
        Texture2D atlas = Resources.Load<Texture2D>("GuildHallGuestsAtlas");
        Sprite room = Resources.Load<Sprite>("GuildHallBackground-EmptyGuests");
        if (atlas == null || room == null || !atlas.isReadable)
            throw new InvalidOperationException("Room/guest imports are incomplete. Run Guild Hall/Prepare Art Imports.");
        // Reject a resized sheet rather than silently slicing neighboring characters.
        if (atlas.width != 1536 || atlas.height != 1024)
            throw new InvalidOperationException($"Guest atlas imported as {atlas.width}x{atlas.height}; expected 1536x1024. NPOT Scale must be None.");
        GameObject art = GameObject.Find("Guild Hall Pixel Art");
        if (art == null) throw new InvalidOperationException("Guild Hall Pixel Art renderer is missing.");
        background = art.GetComponent<SpriteRenderer>();
        previousBackground = background.sprite;
        previousScale = art.transform.localScale;
        background.sprite = room;
        background.transform.localScale = new Vector3(Width / Ppu / room.bounds.size.x, Height / Ppu / room.bounds.size.y, 1);
        background.transform.position = Vector3.zero;
        background.sortingOrder = -100;
        root = new GameObject("Guild Hall - Seated Guests and Table Fronts");
        Portraits = new Texture2D[GuestCount];
        guestSprites = new Sprite[GuestCount];
        artwork = new int[GuestCount];
        Renderers = new SpriteRenderer[GuestCount];
        motions = new GuestSpriteMotion[GuestCount];
        Color32[] all = atlas.GetPixels32();
        int cellW = atlas.width / 3, cellH = atlas.height / 2;
        for (int i = 0; i < GuestCount; i++)
        {
            // Copy a whole cell to its own texture: no shared UVs in world or popup.
            int x0 = i % 3 * cellW, y0 = (1 - i / 3) * cellH;
            var pixels = new Color32[cellW * cellH];
            for (int y = 0; y < cellH; y++)
                Array.Copy(all, (y0 + y) * atlas.width + x0, pixels, y * cellW, cellW);
            var texture = new Texture2D(cellW, cellH, TextureFormat.RGBA32, false) {
                name = "Seated guest " + i, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels); texture.Apply(); owned.Add(texture); Portraits[i] = texture;
            Vector2 pivot = new Vector2(Hips[i].x / 512f, 1 - Hips[i].y / 512f);
            Sprite sprite = Sprite.Create(texture, new Rect(0,0,cellW,cellH), pivot, Ppu, 0, SpriteMeshType.FullRect);
            owned.Add(sprite);
            guestSprites[i] = sprite;
            artwork[i] = i;
            GameObject actor = new GameObject("Seated Guest " + i);
            actor.transform.SetParent(root.transform, false);
            Renderers[i] = actor.AddComponent<SpriteRenderer>(); Renderers[i].sprite = sprite;
            motions[i] = actor.AddComponent<GuestSpriteMotion>();
        }
        BuildTableFronts(room.texture);
        BuildGuildmaster(room.texture);
        ready = true;
        Reseat(UnityEngine.Random.Range(1, int.MaxValue));
        FitCamera();
    }

    public void Reseat(int seed)
    {
        var allGuests = new int[GuestCount];
        for (int i = 0; i < GuestCount; i++) allGuests[i] = i;
        Reseat(seed, allGuests);
    }

    public void ConfigureGuests(int[] portraitIndices)
    {
        if (!ready) return;
        var renderers = Renderers;
        int oldCount = renderers.Length;
        Array.Resize(ref renderers, portraitIndices.Length);
        Array.Resize(ref motions, portraitIndices.Length);
        Renderers = renderers;
        artwork = (int[])portraitIndices.Clone();
        for(int i=0;i<artwork.Length;i++)
        {
            artwork[i] = Mathf.Clamp(artwork[i],0,GuestCount-1);
            if(i>=oldCount)
            {
                var actor = new GameObject("Seated Guest " + i);
                actor.transform.SetParent(root.transform,false);
                Renderers[i]=actor.AddComponent<SpriteRenderer>();
                motions[i]=actor.AddComponent<GuestSpriteMotion>();
            }
            Renderers[i].sprite=guestSprites[artwork[i]];
        }
    }

    public void Reseat(int seed, IReadOnlyList<int> activeGuests, int capacity = GuestCount)
    {
        // Expansion reveals furniture seats in order: 2, 3, 4, 5, then 6.
        int usableSeats = Mathf.Clamp(capacity, 0, Mathf.Min(GuestCount, Seats.Length));
        var order = new int[usableSeats]; for (int i=0; i<order.Length; i++) order[i]=i;
        var random = new System.Random(seed);
        for (int i=order.Length-1; i>0; i--) { int j=random.Next(i+1); int t=order[i]; order[i]=order[j]; order[j]=t; }
        SeatForGuest = new int[Renderers.Length];
        for (int i=0; i<Renderers.Length; i++) { SeatForGuest[i] = -1; Renderers[i].enabled = false; }
        for (int seatIndex=0; seatIndex<activeGuests.Count && seatIndex<order.Length; seatIndex++)
        {
            int guest = activeGuests[seatIndex];
            if (guest < 0 || guest >= Renderers.Length || SeatForGuest[guest]>=0) continue;
            int seatIndexShuffled = order[seatIndex];
            Seat seat = Seats[seatIndexShuffled];
            SeatForGuest[guest] = seatIndexShuffled;
            Renderers[guest].enabled = true;
            Renderers[guest].flipX = seat.faceLeft != SourceFacesLeft[artwork[guest]];
            Renderers[guest].sortingOrder = seat.order;
            motions[guest].Initialize(Point(seat.hip), Vector3.one * seat.scale, guest * .8f);
            motions[guest].SetHovered(false);
        }
    }

    public void SetHovered(int guest)
    { if (motions != null) for (int i=0; i<motions.Length; i++) motions[i].SetHovered(i==guest); }

    public void SetGuildmasterHovered(bool value)
    { if (guildmasterMotion != null) guildmasterMotion.SetHovered(value); }

    public bool HitTestGuildmaster(Vector2 screenPoint)
    {
        if (!ready || guildmasterRenderer == null) return false;
        Vector3 world = roomCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 10));
        Vector3 local = guildmasterRenderer.transform.InverseTransformPoint(world);
        Vector2 pixel = GuildmasterAnchor + new Vector2(local.x * Ppu, -local.y * Ppu);
        bool inside = false;
        for (int i = 0, j = GuildmasterOutline.Length - 1; i < GuildmasterOutline.Length; j = i++)
        {
            Vector2 a = GuildmasterOutline[i], b = GuildmasterOutline[j];
            if ((a.y > pixel.y) != (b.y > pixel.y) && pixel.x < (b.x-a.x)*(pixel.y-a.y)/(b.y-a.y)+a.x)
                inside = !inside;
        }
        return inside;
    }

    public Rect GuildmasterScreenRect()
    {
        Bounds b = guildmasterRenderer.bounds;
        Vector3 a = roomCamera.WorldToScreenPoint(b.min), z = roomCamera.WorldToScreenPoint(b.max);
        return new Rect(a.x, Screen.height-z.y, z.x-a.x, z.y-a.y);
    }

    private void BuildGuildmaster(Texture2D texture)
    {
        // Sample a neighbouring stretch of the same shelf only under the host's
        // silhouette, so a small hover displacement doesn't reveal a second face.
        MakeGuildmasterLayer(texture, "Shelf behind guildmaster", -99, 154);
        GameObject actor = MakeGuildmasterLayer(texture, "Guildmaster interaction", -98, 0);
        guildmasterRenderer = actor.GetComponent<MeshRenderer>();
        guildmasterMotion = actor.AddComponent<GuestSpriteMotion>();
        guildmasterMotion.Initialize(Point(GuildmasterAnchor), Vector3.one, 0);
    }

    private GameObject MakeGuildmasterLayer(Texture2D texture, string label, int order, float sourceOffset)
    {
        // A triangle fan around the interior of the silhouette preserves the
        // original pixels; the bartender remains anchored behind the bar.
        int count = GuildmasterOutline.Length;
        Vector2 center = new Vector2(850, 355);
        var vertices = new Vector3[count + 1];
        var uv = new Vector2[count + 1];
        var triangles = new int[count * 3];
        for (int i = 0; i <= count; i++)
        {
            Vector2 point = i == count ? center : GuildmasterOutline[i];
            vertices[i] = Point(point) - Point(GuildmasterAnchor);
            uv[i] = new Vector2((point.x + sourceOffset) / Width, 1 - point.y / Height);
            if (i < count)
            { triangles[i*3] = count; triangles[i*3+1] = (i+1)%count; triangles[i*3+2] = i; }
        }
        var mesh = new Mesh { name = label, vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateBounds(); owned.Add(mesh);
        var material = new Material(Shader.Find("Sprites/Default")) { mainTexture = texture }; owned.Add(material);
        var actor = new GameObject(label);
        actor.transform.SetParent(root.transform, false);
        actor.transform.position = Point(GuildmasterAnchor);
        actor.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = actor.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material; renderer.sortingOrder = order;
        return actor;
    }

    public int HitTest(Vector2 screenPoint)
    {
        if (!ready) return -1;
        Vector3 world = roomCamera.ScreenToWorldPoint(new Vector3(screenPoint.x,screenPoint.y,10));
        for (int i=Renderers.Length-1; i>=0; i--)
        {
            var sr = Renderers[i];
            if (!sr.enabled || SeatForGuest[i] < 0) continue;
            Vector3 local = sr.transform.InverseTransformPoint(world);
            float px = local.x * Ppu * (sr.flipX ? -1 : 1) + sr.sprite.pivot.x;
            float py = local.y * Ppu + sr.sprite.pivot.y;
            Texture2D portrait = Portraits[artwork[i]];
            if (px<0 || py<0 || px>=portrait.width || py>=portrait.height) continue;
            if (portrait.GetPixel((int)px,(int)py).a < .3f) continue;
            // A table-covered lower body must not steal clicks from the room.
            Seat seat = Seats[SeatForGuest[i]];
            if (world.y < Point(seat.hip).y + .20f) continue;
            return i;
        }
        return -1;
    }

    public Rect ScreenRect(int guest)
    {
        Bounds b=Renderers[guest].bounds;
        Vector3 a=roomCamera.WorldToScreenPoint(b.min), z=roomCamera.WorldToScreenPoint(b.max);
        return new Rect(a.x, Screen.height-z.y, z.x-a.x, z.y-a.y);
    }

    private void LateUpdate() { if (ready) FitCamera(); }
    public void FitCamera()
    {
        roomCamera.orthographic=true;
        roomCamera.orthographicSize=Mathf.Max(Height/Ppu/2,Width/Ppu/2/roomCamera.aspect);
        roomCamera.transform.position=new Vector3(0,0,-10);
    }
    public static Vector3 Point(Vector2 p) => new Vector3((p.x-Width/2)/Ppu,(Height/2-p.y)/Ppu,0);

    private void BuildTableFronts(Texture2D texture)
    {
        // These meshes sample the untouched room art. They cover guests with the
        // actual table top/front, leaving heads, capes and legs visible beside it.
        Front(texture, "Left tabletop", 12, new Vector2[]{new Vector2(157,606),new Vector2(184,590),new Vector2(252,578),new Vector2(349,578),new Vector2(433,589),new Vector2(472,608),new Vector2(470,638),new Vector2(434,655),new Vector2(346,668),new Vector2(254,666),new Vector2(186,649),new Vector2(157,634)});
        Front(texture, "Left pedestal", 12, new Vector2[]{new Vector2(255,645),new Vector2(391,645),new Vector2(388,718),new Vector2(367,741),new Vector2(285,741),new Vector2(256,724)});
        Front(texture, "Middle tabletop", 32, new Vector2[]{new Vector2(692,698),new Vector2(730,682),new Vector2(800,670),new Vector2(911,670),new Vector2(1003,686),new Vector2(1048,706),new Vector2(1047,735),new Vector2(1004,753),new Vector2(920,766),new Vector2(812,765),new Vector2(727,751),new Vector2(692,733)});
        Front(texture, "Middle pedestal", 32, new Vector2[]{new Vector2(795,746),new Vector2(949,746),new Vector2(945,833),new Vector2(909,858),new Vector2(838,858),new Vector2(801,842)});
        Front(texture, "Right tabletop", 12, new Vector2[]{new Vector2(1233,575),new Vector2(1272,559),new Vector2(1353,552),new Vector2(1437,558),new Vector2(1509,576),new Vector2(1522,592),new Vector2(1517,619),new Vector2(1465,635),new Vector2(1366,642),new Vector2(1276,632),new Vector2(1233,612)});
        Front(texture, "Right pedestal", 12, new Vector2[]{new Vector2(1307,619),new Vector2(1439,619),new Vector2(1435,681),new Vector2(1405,700),new Vector2(1343,700),new Vector2(1314,686)});
    }

    private void Front(Texture2D texture, string label, int order, Vector2[] polygon)
    {
        Vector3[] vertices=new Vector3[polygon.Length]; Vector2[] uv=new Vector2[polygon.Length];
        for(int i=0;i<polygon.Length;i++) { vertices[i]=Point(polygon[i]); uv[i]=new Vector2(polygon[i].x/Width,1-polygon[i].y/Height); }
        int[] tris=new int[(polygon.Length-2)*3];
        for(int i=0;i<polygon.Length-2;i++) { tris[i*3]=0; tris[i*3+1]=i+2; tris[i*3+2]=i+1; }
        var mesh=new Mesh {name=label,vertices=vertices,uv=uv,triangles=tris}; mesh.RecalculateBounds(); owned.Add(mesh);
        var material=new Material(Shader.Find("Sprites/Default")) {mainTexture=texture}; owned.Add(material);
        var go=new GameObject(label); go.transform.SetParent(root.transform,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.sortingOrder=order;
    }

    private void OnDestroy()
    {
        if (background != null && previousBackground != null) { background.sprite=previousBackground; background.transform.localScale=previousScale; }
        if (root != null) Dispose(root);
        foreach(var asset in owned) if(asset != null) Dispose(asset);
    }
    private static void Dispose(UnityEngine.Object value) { if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
}
