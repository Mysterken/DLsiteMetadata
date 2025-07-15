using System.Collections.Generic;
using DLsiteMetadata.Enums;

namespace DLsiteMetadata;

public static class TranslationDictionary
{
    public static readonly Dictionary<SupportedLanguages, string> ReleaseDate = new()
    {
        { SupportedLanguages.ja_JP, "販売日" },
        { SupportedLanguages.en_US, "Release date" },
        { SupportedLanguages.zh_CN, "贩卖日" },
        { SupportedLanguages.zh_TW, "販賣日" },
        { SupportedLanguages.ko_KR, "판매일" },
        { SupportedLanguages.es_ES, "Lanzamiento" },
        { SupportedLanguages.de_DE, "Veröffentlicht" },
        { SupportedLanguages.fr_FR, "Date de sortie" },
        { SupportedLanguages.id_ID, "Tanggal rilis" },
        { SupportedLanguages.it_IT, "Data di rilascio" },
        { SupportedLanguages.pt_BR, "Lançamento" },
        { SupportedLanguages.sv_SE, "Utgivningsdatum" },
        { SupportedLanguages.th_TH, "วันที่ขาย" },
        { SupportedLanguages.vi_VN, "Ngày phát hành" }
    };

    public static readonly Dictionary<SupportedLanguages, string> UpdateDate = new()
    {
        { SupportedLanguages.ja_JP, "更新情報" },
        { SupportedLanguages.en_US, "Update information" },
        { SupportedLanguages.zh_CN, "更新信息" },
        { SupportedLanguages.zh_TW, "更新資訊" },
        { SupportedLanguages.ko_KR, "갱신 정보" },
        { SupportedLanguages.es_ES, "Actualizar información" },
        { SupportedLanguages.de_DE, "Aktualisierungen" },
        { SupportedLanguages.fr_FR, "Mise à jour des informations" },
        { SupportedLanguages.id_ID, "Perbarui informasi" },
        { SupportedLanguages.it_IT, "Aggiorna informazioni" },
        { SupportedLanguages.pt_BR, "Atualizar informações" },
        { SupportedLanguages.sv_SE, "Uppdatera information" },
        { SupportedLanguages.th_TH, "ข้อมูลอัปเดต" },
        { SupportedLanguages.vi_VN, "Thông tin cập nhật" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Series = new()
    {
        { SupportedLanguages.ja_JP, "シリーズ名" },
        { SupportedLanguages.en_US, "Series name" },
        { SupportedLanguages.zh_CN, "系列名" },
        { SupportedLanguages.zh_TW, "系列名" },
        { SupportedLanguages.ko_KR, "시리즈명" },
        { SupportedLanguages.es_ES, "Serie" },
        { SupportedLanguages.de_DE, "Serie" },
        { SupportedLanguages.fr_FR, "Série" },
        { SupportedLanguages.id_ID, "Nama seri" },
        { SupportedLanguages.it_IT, "Serie" },
        { SupportedLanguages.pt_BR, "Série" },
        { SupportedLanguages.sv_SE, "Serier" },
        { SupportedLanguages.th_TH, "ชื่อซีรี่ส์" },
        { SupportedLanguages.vi_VN, "Bộ truyện" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Scenario = new()
    {
        { SupportedLanguages.ja_JP, "シナリオ" },
        { SupportedLanguages.en_US, "Scenario" },
        { SupportedLanguages.zh_CN, "剧情" },
        { SupportedLanguages.zh_TW, "劇本" },
        { SupportedLanguages.ko_KR, "시나리오" },
        { SupportedLanguages.es_ES, "Guión" },
        { SupportedLanguages.de_DE, "Szenario" },
        { SupportedLanguages.fr_FR, "Scénario" },
        { SupportedLanguages.id_ID, "Skenario" },
        { SupportedLanguages.it_IT, "Scenario" },
        { SupportedLanguages.pt_BR, "Cenário" },
        { SupportedLanguages.sv_SE, "Scenario" },
        { SupportedLanguages.th_TH, "บทละคร" },
        { SupportedLanguages.vi_VN, "Kịch bản" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Illustration = new()
    {
        { SupportedLanguages.ja_JP, "イラスト" },
        { SupportedLanguages.en_US, "Illustration" },
        { SupportedLanguages.zh_CN, "插画" },
        { SupportedLanguages.zh_TW, "插畫" },
        { SupportedLanguages.ko_KR, "일러스트" },
        { SupportedLanguages.es_ES, "Ilustración" },
        { SupportedLanguages.de_DE, "AbbilDung" },
        { SupportedLanguages.fr_FR, "Illustration" },
        { SupportedLanguages.id_ID, "Ilustrasi" },
        { SupportedLanguages.it_IT, "Illustrazione" },
        { SupportedLanguages.pt_BR, "Ilustração" },
        { SupportedLanguages.sv_SE, "Illustration" },
        { SupportedLanguages.th_TH, "ภาพประกอบ" },
        { SupportedLanguages.vi_VN, "Tranh minh họa" }
    };

    public static readonly Dictionary<SupportedLanguages, string> VoiceActor = new()
    {
        { SupportedLanguages.ja_JP, "声優" },
        { SupportedLanguages.en_US, "Voice Actor" },
        { SupportedLanguages.zh_CN, "声优" },
        { SupportedLanguages.zh_TW, "聲優" },
        { SupportedLanguages.ko_KR, "성우" },
        { SupportedLanguages.es_ES, "Doblador" },
        { SupportedLanguages.de_DE, "Synchronsprecher" },
        { SupportedLanguages.fr_FR, "Doubleur" },
        { SupportedLanguages.id_ID, "Pengisi suara" },
        { SupportedLanguages.it_IT, "Doppiatore/Doppiatrice" },
        { SupportedLanguages.pt_BR, "Ator de voz" },
        { SupportedLanguages.sv_SE, "Röstskådespelare" },
        { SupportedLanguages.th_TH, "นักพากย์" },
        { SupportedLanguages.vi_VN, "Diễn viên lồng tiếng" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Music = new()
    {
        { SupportedLanguages.ja_JP, "音楽" },
        { SupportedLanguages.en_US, "Music" },
        { SupportedLanguages.zh_CN, "音乐" },
        { SupportedLanguages.zh_TW, "音樂" },
        { SupportedLanguages.ko_KR, "음악" },
        { SupportedLanguages.es_ES, "Música" },
        { SupportedLanguages.de_DE, "Musik" },
        { SupportedLanguages.fr_FR, "Musique" },
        { SupportedLanguages.id_ID, "Musik" },
        { SupportedLanguages.it_IT, "Musica" },
        { SupportedLanguages.pt_BR, "Música" },
        { SupportedLanguages.sv_SE, "musik" },
        { SupportedLanguages.th_TH, "ดนตรี" },
        { SupportedLanguages.vi_VN, "Âm nhạc" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Author = new()
    {
        { SupportedLanguages.ja_JP, "作者" },
        { SupportedLanguages.en_US, "Author" },
        { SupportedLanguages.zh_CN, "作者" },
        { SupportedLanguages.zh_TW, "作者" },
        { SupportedLanguages.ko_KR, "저자" },
        { SupportedLanguages.es_ES, "Autor" },
        { SupportedLanguages.de_DE, "Autor" },
        { SupportedLanguages.fr_FR, "Auteur" },
        { SupportedLanguages.id_ID, "Penulis" },
        { SupportedLanguages.it_IT, "Autore" },
        { SupportedLanguages.pt_BR, "Autor" },
        { SupportedLanguages.sv_SE, "Författare" },
        { SupportedLanguages.th_TH, "ผู้เขียน" },
        { SupportedLanguages.vi_VN, "Tác giả" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Age = new()
    {
        { SupportedLanguages.ja_JP, "年齢指定" },
        { SupportedLanguages.en_US, "Age" },
        { SupportedLanguages.zh_CN, "年龄指定" },
        { SupportedLanguages.zh_TW, "年齡指定" },
        { SupportedLanguages.ko_KR, "연령 지정" },
        { SupportedLanguages.es_ES, "Edad" },
        { SupportedLanguages.de_DE, "Altersfreigabe" },
        { SupportedLanguages.fr_FR, "Âge" },
        { SupportedLanguages.id_ID, "Batas usia" },
        { SupportedLanguages.it_IT, "Età" },
        { SupportedLanguages.pt_BR, "Idade" },
        { SupportedLanguages.sv_SE, "Ålder" },
        { SupportedLanguages.th_TH, "การกำหนดอายุ" },
        { SupportedLanguages.vi_VN, "Độ tuổi chỉ định" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Miscellaneous = new()
    {
        { SupportedLanguages.ja_JP, "その他" },
        { SupportedLanguages.en_US, "Miscellaneous" },
        { SupportedLanguages.zh_CN, "其他" },
        { SupportedLanguages.zh_TW, "其他" },
        { SupportedLanguages.ko_KR, "기타" },
        { SupportedLanguages.es_ES, "Misceláneos" },
        { SupportedLanguages.de_DE, "Sonstiges" },
        { SupportedLanguages.fr_FR, "Autre" },
        { SupportedLanguages.id_ID, "Lainnya" },
        { SupportedLanguages.it_IT, "Altri" },
        { SupportedLanguages.pt_BR, "Diversos" },
        { SupportedLanguages.sv_SE, "Andra" },
        { SupportedLanguages.th_TH, "อื่น ๆ" },
        { SupportedLanguages.vi_VN, "Loại khác" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Genre = new()
    {
        { SupportedLanguages.ja_JP, "ジャンル" },
        { SupportedLanguages.en_US, "Genre" },
        { SupportedLanguages.zh_CN, "分类" },
        { SupportedLanguages.zh_TW, "分類" },
        { SupportedLanguages.ko_KR, "장르" },
        { SupportedLanguages.es_ES, "Género" },
        { SupportedLanguages.de_DE, "Genre" },
        { SupportedLanguages.fr_FR, "Genre" },
        { SupportedLanguages.id_ID, "Genre" },
        { SupportedLanguages.it_IT, "Genere" },
        { SupportedLanguages.pt_BR, "Gênero" },
        { SupportedLanguages.sv_SE, "Genre" },
        { SupportedLanguages.th_TH, "ประเภท" },
        { SupportedLanguages.vi_VN, "Thể loại" }
    };

    public static readonly Dictionary<SupportedLanguages, string> ProductFormat = new()
    {
        { SupportedLanguages.ja_JP, "作品形式" },
        { SupportedLanguages.en_US, "Product format" },
        { SupportedLanguages.zh_CN, "作品类型" },
        { SupportedLanguages.zh_TW, "作品形式" },
        { SupportedLanguages.ko_KR, "작품 형식" },
        { SupportedLanguages.es_ES, "Formato del Producto" },
        { SupportedLanguages.de_DE, "Titelformat" },
        { SupportedLanguages.fr_FR, "Format du produit" },
        { SupportedLanguages.id_ID, "Format Karya" },
        { SupportedLanguages.it_IT, "Formato dell'opera" },
        { SupportedLanguages.pt_BR, "Formato do produto" },
        { SupportedLanguages.sv_SE, "Produktformat" },
        { SupportedLanguages.th_TH, "รูปแบบผลงาน" },
        { SupportedLanguages.vi_VN, "Định dạng tác phẩm" }
    };

    public static readonly Dictionary<SupportedLanguages, string> FileFormat = new()
    {
        { SupportedLanguages.ja_JP, "ファイル形式" },
        { SupportedLanguages.en_US, "File format" },
        { SupportedLanguages.zh_CN, "文件形式" },
        { SupportedLanguages.zh_TW, "檔案形式" },
        { SupportedLanguages.ko_KR, "파일 형식" },
        { SupportedLanguages.es_ES, "Formato del Archivo" },
        { SupportedLanguages.de_DE, "Dateiformat" },
        { SupportedLanguages.fr_FR, "Format de fichier" },
        { SupportedLanguages.id_ID, "Format file" },
        { SupportedLanguages.it_IT, "Formato del file" },
        { SupportedLanguages.pt_BR, "Formato do arquivo" },
        { SupportedLanguages.sv_SE, "Filformat" },
        { SupportedLanguages.th_TH, "รูปแบบไฟล์" },
        { SupportedLanguages.vi_VN, "Định dạng tệp tin" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Filesize = new()
    {
        { SupportedLanguages.ja_JP, "ファイル容量" },
        { SupportedLanguages.en_US, "File size" },
        { SupportedLanguages.zh_CN, "文件大小" },
        { SupportedLanguages.zh_TW, "檔案大小" },
        { SupportedLanguages.ko_KR, "파일 크기" },
        { SupportedLanguages.es_ES, "Tamaño del archivo" },
        { SupportedLanguages.de_DE, "Dateigröße" },
        { SupportedLanguages.fr_FR, "Taille du fichier" },
        { SupportedLanguages.id_ID, "Ukuran file" },
        { SupportedLanguages.it_IT, "Dimensione del file" },
        { SupportedLanguages.pt_BR, "Tamanho do arquivo" },
        { SupportedLanguages.sv_SE, "Filstorlek" },
        { SupportedLanguages.th_TH, "ขนาดไฟล์" },
        { SupportedLanguages.vi_VN, "Kích thước tập tin" }
    };

    public static readonly Dictionary<SupportedLanguages, string> AllAges = new()
    {
        { SupportedLanguages.ja_JP, "全年齢" },
        { SupportedLanguages.en_US, "All ages" },
        { SupportedLanguages.zh_CN, "全年龄" },
        { SupportedLanguages.zh_TW, "全年齡" },
        { SupportedLanguages.ko_KR, "전연령" },
        { SupportedLanguages.es_ES, "Todas las edades" },
        { SupportedLanguages.de_DE, "Alle Altersgruppen" },
        { SupportedLanguages.fr_FR, "Tous âges" },
        { SupportedLanguages.id_ID, "Semua umur" },
        { SupportedLanguages.it_IT, "Tuttel le età" },
        { SupportedLanguages.pt_BR, "Todas as idades" },
        { SupportedLanguages.sv_SE, "Alla åldrar" },
        { SupportedLanguages.th_TH, "ทุกวัย" },
        { SupportedLanguages.vi_VN, "Tất cả độ tuổi" }
    };

    public static readonly Dictionary<SupportedLanguages, string> R15 = new()
    {
        { SupportedLanguages.ja_JP, "R-15" },
        { SupportedLanguages.en_US, "R-15" },
        { SupportedLanguages.zh_CN, "R-15" },
        { SupportedLanguages.zh_TW, "R-15" },
        { SupportedLanguages.ko_KR, "R-15" },
        { SupportedLanguages.es_ES, "R-15" },
        { SupportedLanguages.de_DE, "R-15" },
        { SupportedLanguages.fr_FR, "R-15" },
        { SupportedLanguages.id_ID, "R-15" },
        { SupportedLanguages.it_IT, "Ragazzi 15+" },
        { SupportedLanguages.pt_BR, "R-15" },
        { SupportedLanguages.sv_SE, "R-15" },
        { SupportedLanguages.th_TH, "R-15" },
        { SupportedLanguages.vi_VN, "R-15" }
    };

    public static readonly Dictionary<SupportedLanguages, string> Adult = new()
    {
        { SupportedLanguages.ja_JP, "R18" },
        { SupportedLanguages.en_US, "R18" },
        { SupportedLanguages.zh_CN, "R18" },
        { SupportedLanguages.zh_TW, "R18" },
        { SupportedLanguages.ko_KR, "R18" },
        { SupportedLanguages.es_ES, "R18" },
        { SupportedLanguages.de_DE, "R18" },
        { SupportedLanguages.fr_FR, "R18" },
        { SupportedLanguages.id_ID, "R18" },
        { SupportedLanguages.it_IT, "18+" },
        { SupportedLanguages.pt_BR, "R18" },
        { SupportedLanguages.sv_SE, "R18" },
        { SupportedLanguages.th_TH, "R18" },
        { SupportedLanguages.vi_VN, "R18" }
    };
}