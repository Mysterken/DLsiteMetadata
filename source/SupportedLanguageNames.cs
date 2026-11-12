using System;
using System.Collections.Generic;
using DLsiteMetadata.Enums;

namespace DLsiteMetadata;

/// <summary>
///     Localized names of the language codes found in the product API's <c>work_languages</c>.
///     The names are not part of the API response, they were collected from DLsite's own listing pages
///     with <c>tools/generate_language_names.py</c>. Each row follows the order of <see cref="SupportedLanguages" />.
/// </summary>
public static class SupportedLanguageNames
{
    private static readonly Dictionary<string, string[]> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JPN"] = ["日本語", "Japanese", "日语", "日文", "일본어", "Japonés", "Japanisch", "Japonais", "Bahasa Jepang", "Giapponese", "Japonês", "japanska", "ภาษาญี่ปุ่น", "Tiếng Nhật"],
        ["ENG"] = ["英語", "English", "英语", "英文", "영어", "Inglés", "Englisch (Sprache)", "Anglais", "Bahasa Inggris", "Inglese", "Inglês", "engelsk", "ภาษาอังกฤษ", "Tiếng Anh"],
        ["CHI_HANS"] = ["中国語(簡体字)", "Chinese Simplified", "中文(简体字)", "中文(簡體字)", "중국어 (간체자)", "Chino Simplificado", "Chinesisch (vereinfacht)", "Chinois (simplifié)", "Bahasa Cina (Sederhana)", "Cinese (caratteri semplificati)", "中国語(簡体字)", "Kinesiska (Förenklad)", "ภาษาจีน (ตัวย่อ)", "Tiếng Trung ( Giản thể)"],
        ["CHI_HANT"] = ["中国語(繁体字)", "Chinese Traditional", "中文(繁体字)", "中文(繁體字)", "중국어 (번체자)", "Chino Tradicional", "Chinesisch (traditionell)", "Chinois (traditionnel)", "Bahasa Cina (Tradisional)", "Cinese (caratteri tradizionali)", "中国語(繁体字)", "Kinesisk (Traditionell)", "ภาษาจีน (ตัวเต็ม)", "Tiếng Trung ( Phồn thể)"],
        ["CHI"] = ["中国語", "Chinese", "中文", "中文", "중국어", "Chino", "Chinesisch ", "Chinois", "Bahasa Cina", "Cinese", "Chinês", "Kinesiska", "ภาษาจีน", "Tiếng Trung"],
        ["KO_KR"] = ["韓国語", "Korean", "韩语", "韓語", "한국어", "Coreano", "Koreanisch", "Coréen", "Bahasa Korea", "Coreano", "Coreano", "koreanska", "ภาษาเกาหลี", "Tiếng Hàn Quốc"],
        ["SPA"] = ["スペイン語", "Spanish", "西班牙语", "西班牙語", "스페인어", "Español", "Spanisch", "Espagnol", "Bahasa Spanyol", "Spagnolo", "Espanhol", "Spanska", "สเปน", "Tiếng Tây Ban Nha"],
        ["GER"] = ["ドイツ語", "German", "德语", "德語", "독일어", "Alemán", "Deutsch", "Allemand", "Bahasa Jerman", "Tedesco", "Alemão", "Tyska", "เยอรมัน", "Tiếng Đức"],
        ["FRE"] = ["フランス語", "French", "法语", "法語", "프랑스어", "Francés", "Französisch", "Français", "Bahasa Prancis", "Francese", "Francês", "Franska", "ฝรั่งเศส", "Tiếng Pháp"],
        ["IND"] = ["インドネシア語", "Indonesian", "印尼语", "印度尼西亞語", "인도네시아어", "Indonesio", "Indonesisch", "Indonésien", "Bahasa Indonesia", "Indonesiano", "Indonésio", "Indonesiska", "อินโดนีเซีย", "Tiếng Indonesia"],
        ["ITA"] = ["イタリア語", "Italian", "意大利语", "義大利語", "이탈리아어", "Italiano", "Italienisch", "Italien", "Bahasa Italia", "Italiano", "Italiano", "Italienska", "อิตาลี", "Tiếng Ý"],
        ["POR"] = ["ポルトガル語", "Portuguese", "葡萄牙语", "葡萄牙語", "포르투갈어", "Portugués", "Portugiesisch", "Portugais", "Bahasa Portugis", "Portoghese", "Português", "Portugisiska", "โปรตุเกส", "Tiếng Bồ Đào Nha"],
        ["SWE"] = ["スウェーデン語", "Swedish", "瑞典语", "瑞典語", "스웨덴어", "Sueco", "Schwedisch", "Suédois", "Bahasa Swedia", "Svedese", "Sueco", "Svenska", "สวีเดน", "Tiếng Thụy Điển"],
        ["THA"] = ["タイ語", "Thai", "泰语", "泰語", "태국어", "Tailandés", "Thailändisch", "Thaïlandais", "Bahasa Thailand", "Tailandese", "Tailandês", "Thailändska", "ไทย", "Tiếng Thái"],
        ["VIE"] = ["ベトナム語", "Vietnamese", "越南语", "越南語", "베트남어", "Vietnamita", "Vietnamesisch", "Vietnamien", "Bahasa Vietnam", "Vietnamita", "Vietnamita", "Vietnamesiska", "เวียดนาม", "Tiếng Việt"],
        ["NM"] = ["言語不要", "Alingual", "无语言限制", "不限語言", "언어 불필요", "Alingüe", "Keine Sprache erforderlich", "Alingue", "Tidak Perlu Bahasa", "Alinguistico", "Sem Idioma", "Inget språk krävs", "ไม่ต้องใช้ภาษา", "Không yêu cầu ngôn ngữ"],
    };

    /// <summary>
    ///     "CHI" is the generic Chinese flag set next to the simplified/traditional ones,
    ///     DLsite does not list it on the product page.
    /// </summary>
    private static readonly HashSet<string> HiddenCodes = new(StringComparer.OrdinalIgnoreCase) { "CHI" };

    public static IReadOnlyCollection<string> Codes => Names.Keys;

    public static bool TryGet(string code, SupportedLanguages language, out string name)
    {
        name = null;

        if (code == null || HiddenCodes.Contains(code) || !Names.TryGetValue(code, out var row)) return false;

        name = row[(int)language];
        return true;
    }

    public static bool IsComplete(string code)
    {
        return Names.TryGetValue(code, out var row) && row.Length == Enum.GetValues(typeof(SupportedLanguages)).Length;
    }
}
