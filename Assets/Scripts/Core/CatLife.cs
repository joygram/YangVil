using System;
using System.Collections.Generic;

namespace YangVil.Core
{
    public enum Personality { Sweet, Shy, Proud, Playful, Glutton }

    public enum Grievance { Food, Bed, Territory, Jealousy }

    public enum Interaction { Groom, Nap, Chase, Reject, Hiss, Fight, MakeUp }

    public class InteractionResult
    {
        public Interaction Kind;
        public Grievance? Cause;
        public string BarkA = "";
        public string BarkB = "";
    }

    public class EpisodeChoice
    {
        public string Label;
        public string After;
        public int Bond;
        public int FriendAffinity;
        public int Coins;
    }

    public class Episode
    {
        public string Id;
        public string Title;
        public Func<Game, OwnedCat, bool> Ready;
        public string[] Lines;
        public EpisodeChoice[] Choices;
    }

    public static class CatLife
    {
        public const int PersonalityCount = 5;
        public const float BondFloor = 20f;
        public const float BondDecayPerHour = 1.5f;
        public const float BreedAffinity = 60f;
        public const float NapAffinity = 30f;
        public const float HissGrievance = 30f;
        public const float FightGrievance = 60f;
        public const float ParentAffinity = 40f;
        public const float PersonalityMutation = 0.15f;
        public const int MemoryLimit = 5;
        public const long ExpeditionCapSeconds = 8 * 3600;
        public const int ExpeditionRunSeconds = 180;
        public const float ExpeditionEfficiency = 0.6f;
        public const long WelcomeAfterSeconds = 6 * 3600;

        public static string Label(Personality p) => p switch
        {
            Personality.Sweet => "다정",
            Personality.Shy => "소심",
            Personality.Proud => "도도",
            Personality.Playful => "장난",
            _ => "먹보"
        };

        public static string Label(Grievance g) => g switch
        {
            Grievance.Food => "밥그릇 다툼",
            Grievance.Bed => "잠자리 다툼",
            Grievance.Territory => "집이 비좁음",
            _ => "질투"
        };

        public static string CauseBark(Grievance g) => g switch
        {
            Grievance.Food => "그거 내 밥이었거든!",
            Grievance.Bed => "거기 내 자리야!",
            Grievance.Territory => "여긴 너무 좁다고!",
            _ => "왜 쟤만 쓰다듬어?"
        };

        public static string Fix(Grievance g) => g switch
        {
            Grievance.Food => "둘 다 배부르게 츄르 주기",
            Grievance.Bed => "다른 집으로 떼어놓기",
            Grievance.Territory => "집 업그레이드로 넓히기",
            _ => "둘 다 공평하게 쓰다듬기"
        };

        public static float AcceptChance(Personality p, float affinity)
        {
            var bias = p switch
            {
                Personality.Sweet => 0.25f,
                Personality.Shy => -0.2f,
                Personality.Proud => -0.15f,
                Personality.Playful => 0.1f,
                _ => 0f
            };
            return Math.Clamp(0.5f + bias + affinity / 200f, 0.05f, 0.95f);
        }

        public static Interaction Pick(Personality p, float affinity, Random rng)
        {
            var groom = p == Personality.Sweet ? 3 : 1;
            var nap = affinity >= NapAffinity ? (p == Personality.Glutton || p == Personality.Shy ? 3 : 1) : 0;
            var chase = p == Personality.Playful ? 3 : 1;
            var roll = rng.Next(groom + nap + chase);
            if (roll < groom) return Interaction.Groom;
            return roll < groom + nap ? Interaction.Nap : Interaction.Chase;
        }

        public static float AffinityGain(Interaction k) => k switch
        {
            Interaction.Groom => 4f,
            Interaction.Nap => 3f,
            Interaction.Chase => 2f,
            Interaction.MakeUp => 8f,
            Interaction.Reject => -1f,
            Interaction.Hiss => -3f,
            Interaction.Fight => -15f,
            _ => 0f
        };

        static readonly Dictionary<string, string[]> Barks = new Dictionary<string, string[]>
        {
            ["pet"] = new[] { "더 쓰다듬어줘~", "흐음, 나쁘지 않네", "깜짝이야… 그래도 좋아", "한 번 더! 한 번 더!", "간식은 없어?" },
            ["feed"] = new[] { "맛있다, 고마워!", "…특별히 먹어줄게", "고마워… 냠", "냠냠 다 먹었다!", "더! 더 줘!" },
            ["wash"] = new[] { "뽀송뽀송~", "흥, 원래 깨끗했거든", "물 무서웠어…", "거품 놀이 재밌다!", "씻으니 배고파" },
            ["groom"] = new[] { "{0}, 털 정리해줄게", "{0}, 가만히 있어 봐", "{0}… 해줘도 돼?", "{0}! 핥핥 공격!", "{0} 털에서 생선 냄새…" },
            ["nap"] = new[] { "{0} 옆이 제일 따뜻해", "{0}, 조금만 붙어도 돼", "{0} 옆이면 안 무서워", "{0}랑 낮잠 대결!", "{0}, 배부르니 졸려…" },
            ["chase"] = new[] { "{0}, 같이 놀자!", "{0}, 잡아보시든가", "{0}… 놀아줄래?", "{0}, 술래 너야!", "{0}, 이기면 간식!" },
            ["rejected"] = new[] { "{0}가 싫대… 시무룩", "흥, {0} 너 없어도 돼", "역시 {0}는 날 싫어해…", "{0} 재미없어!", "{0} 치사해…" },
            ["refuse"] = new[] { "지금은 좀…", "귀찮게 하지 마", "히익, 다가오지 마…", "나중에 놀자!", "밥 먹는 중이야" },
            ["makeup"] = new[] { "{0}, 아까는 미안", "{0}, 이번만 봐줄게", "{0}… 우리 화해할래?", "{0}, 다시 놀자!", "{0}, 간식 나눠줄게" },
            ["welcome"] = new[] { "보고 싶었어!", "늦었잖아… 흥", "어디 갔었어…", "왔다! 놀아줘!", "밥은?! 밥!" },
            ["expedition"] = new[] { "모험 다녀왔어!", "까마귀쯤이야 가볍지", "무서웠지만 버텼어", "들쥐 엄청 쫓았다!", "간식 주워왔어!" },
            ["date"] = new[] { "{0}랑 오래 같이 있고 싶어", "{0}라면 인정해줄게", "{0} 옆이면 용기가 나", "{0}, 우리 가족 만들자!", "{0}랑 밥 나눠 먹을래" },
            ["dateNo"] = new[] { "{0}랑은 아직…", "{0}? 어림없어", "{0}는 아직 무서워…", "{0}랑은 친구가 좋아", "{0}는 내 밥을 뺏었어" },
        };

        public static string Bark(Personality p, string key, string other = "")
        {
            var lines = Barks[key];
            return string.Format(lines[(int)p % lines.Length], other);
        }

        static bool BondAt(OwnedCat c, Personality p, float bond) => (Personality)c.personality == p && c.bond >= bond;

        static Episode Personal(Personality p, int step, string title, string[] lines, params EpisodeChoice[] choices) => new Episode
        {
            Id = $"p{(int)p}_{step}",
            Title = title,
            Ready = (g, c) => BondAt(c, p, 30 * step),
            Lines = lines,
            Choices = choices
        };

        static EpisodeChoice C(string label, string after, int bond = 0, int friend = 0, int coins = 0) =>
            new EpisodeChoice { Label = label, After = after, Bond = bond, FriendAffinity = friend, Coins = coins };

        public static readonly Episode[] Episodes =
        {
            Personal(Personality.Sweet, 1, "아침 인사", new[] { "{0}가 머리를 비비며 깨운다.", "\"좋은 아침! 오늘도 같이 있자\"" },
                C("꼭 안아준다", "{0}가 골골송을 부른다.", bond: 6), C("간식으로 답한다", "{0}가 냠냠 먹고 꼬리를 세운다.", bond: 3, coins: 10)),
            Personal(Personality.Sweet, 2, "친구 돌보기", new[] { "{0}가 기운 없는 친구 곁을 지킨다.", "\"내가 털 정리해주면 나아질 거야\"" },
                C("함께 돌본다", "둘이 나란히 잠들었다.", bond: 5, friend: 10), C("칭찬해준다", "{0}가 뿌듯해한다.", bond: 8)),
            Personal(Personality.Sweet, 3, "선물", new[] { "{0}가 입에 무언가를 물고 왔다.", "반짝이는 생선 코인이다!" },
                C("고맙다고 쓰다듬는다", "{0}가 세상을 다 가진 표정이다.", bond: 6, coins: 50), C("돌려준다", "{0}가 고개를 갸웃한다.", bond: 2)),
            Personal(Personality.Shy, 1, "상자 속", new[] { "{0}가 상자 안에서 눈만 내놓고 있다.", "\"…나와도 돼?\"" },
                C("기다려준다", "{0}가 조심조심 나와 손에 코를 댄다.", bond: 8), C("장난감을 흔든다", "{0}가 놀라 다시 숨었다.", bond: -2)),
            Personal(Personality.Shy, 2, "천둥 치는 밤", new[] { "천둥소리에 {0}가 떨고 있다.", "\"무서워… 옆에 있어 줘\"" },
                C("곁에서 재운다", "{0}가 처음으로 배를 보이며 잠들었다.", bond: 10), C("담요를 덮어준다", "{0}가 담요 속에서 골골거린다.", bond: 6)),
            Personal(Personality.Shy, 3, "첫 마중", new[] { "문을 열자 {0}가 먼저 달려 나왔다.", "\"기다렸어\"" },
                C("이름을 불러준다", "{0}가 꼬리를 바짝 세운다.", bond: 8), C("간식 파티", "{0}가 친구들까지 불러온다.", bond: 4, friend: 8)),
            Personal(Personality.Proud, 1, "높은 자리", new[] { "{0}가 지붕 위에서 내려다본다.", "\"여기가 내 자리야. 알지?\"" },
                C("인정해준다", "{0}가 만족스럽게 하품한다.", bond: 6), C("내려오라고 한다", "{0}가 못 들은 척한다.", bond: -1)),
            Personal(Personality.Proud, 2, "몰래 응석", new[] { "아무도 없을 때 {0}가 무릎에 올라왔다.", "\"…말하지 마. 비밀이야\"" },
                C("비밀을 지킨다", "{0}가 조용히 골골거린다.", bond: 10), C("친구들에게 자랑한다", "{0}가 흥 하고 도망갔다.", bond: -3, friend: 3)),
            Personal(Personality.Proud, 3, "대장 선언", new[] { "{0}가 마을 고양이들 앞에 섰다.", "\"이 마을은 내가 지킨다\"" },
                C("박수 쳐준다", "고양이들이 {0}를 따라다닌다.", bond: 6, friend: 10), C("같이 순찰한다", "{0}가 뒤를 자꾸 돌아본다.", bond: 9)),
            Personal(Personality.Playful, 1, "털실 대참사", new[] { "{0}가 털실을 온 집에 풀어놨다.", "\"이거 봐! 거미줄이다!\"" },
                C("같이 논다", "둘 다 털실에 감겼다.", bond: 7), C("치우게 한다", "{0}가 털실 공을 굴려 도망간다.", bond: 1)),
            Personal(Personality.Playful, 2, "숨바꼭질", new[] { "{0}가 사라졌다!", "어디선가 꼬리 끝만 보인다." },
                C("못 찾은 척한다", "{0}가 신나서 튀어나왔다.", bond: 8), C("바로 찾는다", "{0}가 분해서 한 판 더 하잔다.", bond: 5)),
            Personal(Personality.Playful, 3, "보물 지도", new[] { "{0}가 낙서투성이 종이를 물고 왔다.", "\"보물 찾으러 가자!\"" },
                C("따라간다", "덤불 밑에서 생선 코인을 찾았다!", bond: 6, coins: 80), C("친구도 부른다", "모두 함께 신나게 뛰어다녔다.", bond: 4, friend: 10)),
            Personal(Personality.Glutton, 1, "밥그릇 소동", new[] { "{0}가 빈 밥그릇을 코로 밀고 있다.", "\"밥… 밥이 사라졌어…\"" },
                C("츄르를 준다", "{0}가 세상에서 제일 행복해 보인다.", bond: 7), C("조금 기다리게 한다", "{0}가 원망스럽게 쳐다본다.", bond: -1)),
            Personal(Personality.Glutton, 2, "나눠 먹기", new[] { "{0}가 간식 앞에서 고민한다.", "옆에 친구가 침을 흘리고 있다." },
                C("나눠 먹자고 한다", "{0}가 반을 양보했다!", bond: 4, friend: 12), C("둘 다 하나씩 준다", "모두 배부르게 먹었다.", bond: 6, friend: 6)),
            Personal(Personality.Glutton, 3, "요리사 고양이", new[] { "{0}가 부엌에서 생선을 지키고 있다.", "\"이건 모두의 저녁이야\"" },
                C("칭찬한다", "{0}가 처음으로 먹을 걸 참았다.", bond: 9), C("같이 저녁 차린다", "마을 잔치가 열렸다.", bond: 5, friend: 8, coins: 30)),
            new Episode
            {
                Id = "first_friend", Title = "첫 친구",
                Ready = (g, c) => g.BestFriend(c) is { } f && g.Affinity(c, f) >= 50,
                Lines = new[] { "{0}가 {1}와 꼭 붙어 다닌다.", "\"{1}는 내 첫 친구야\"" },
                Choices = new[] { C("둘에게 간식을 준다", "둘이 사이좋게 나눠 먹었다.", bond: 4, friend: 10), C("사진을 찍는다", "{0}가 {1} 옆에서 포즈를 잡는다.", bond: 6) }
            },
            new Episode
            {
                Id = "make_up", Title = "화해",
                Ready = (g, c) => g.Relations(c).Exists(r => r.lastCause >= 0 && r.madeUp),
                Lines = new[] { "싸웠던 {0}와 {1}가 서로 눈치를 본다.", "{0}가 먼저 다가가 코를 맞댔다." },
                Choices = new[] { C("칭찬해준다", "둘 사이가 한결 가까워졌다.", bond: 5, friend: 12), C("조용히 지켜본다", "둘이 나란히 앉았다.", friend: 8) }
            },
            new Episode
            {
                Id = "family", Title = "엄마 아빠",
                Ready = (g, c) => !string.IsNullOrEmpty(c.parentA),
                Lines = new[] { "{0}가 부모 곁을 졸졸 따라다닌다.", "\"나도 크면 모험 갈 거야!\"" },
                Choices = new[] { C("응원해준다", "{0}가 힘차게 야옹 했다.", bond: 8), C("가족 사진", "온 가족이 한 프레임에 들어왔다.", bond: 4, friend: 10) }
            },
            new Episode
            {
                Id = "rain_kitten", Title = "비 오는 날",
                Ready = (g, c) => c.bond >= 40 && g.Data.cats.Count >= 2,
                Lines = new[] { "비 오는 골목에서 {0}가 젖은 새끼 고양이를 데려왔다.", "\"얘 집이 없대… 우리 마을에 두면 안 돼?\"" },
                Choices = new[] { C("마을 입구에 보금자리를 만든다", "새끼 고양이가 고맙다며 코인을 물어다 줬다.", bond: 6, coins: 60), C("주인을 찾아준다", "{0}가 뿌듯하게 돌아왔다.", bond: 8) }
            },
            new Episode
            {
                Id = "crow_thief", Title = "생선 도둑 까마귀",
                Ready = (g, c) => g.Data.stages.Count >= 2 && c.bond >= 50,
                Lines = new[] { "까마귀가 마을 생선을 물고 날아갔다!", "{0}가 털을 세웠다. \"모험 게이트 너머로 갔어!\"" },
                Choices = new[] { C("같이 쫓는다", "생선을 되찾았다!", bond: 6, coins: 40), C("다음 모험에서 혼내주자", "{0}가 결의에 찬 눈빛이다.", bond: 8) }
            },
        };
    }
}
