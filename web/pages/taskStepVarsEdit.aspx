<%@ Page Language="C#" AutoEventWireup="true" Inherits="Web.pages.taskStepVarsEdit"
    CodeBehind="taskStepVarsEdit.aspx.cs" MasterPageFile="~/pages/popupwindow.master" %>

<asp:Content ID="cHead" ContentPlaceHolderID="phHead" runat="server">
    <link type="text/css" href="../style/taskEdit.css" rel="stylesheet" />
    <link type="text/css" href="../style/taskSteps.css" rel="stylesheet" />
    <script type="text/javascript" src="../script/taskedit/taskStepVarsEdit.js"></script>
<style type="text/css">
        .delimiter_label
        {
            list-style: none;
            display: inline-block;
            width: 28px;
            border: 1px solid #CCC;
            text-align: center;
            border-radius: 6px;
        }
        .delimiter
        {
            font-family: Courier New;
            font-size: 8pt;
            color: #11A;
            list-style: none;
            display: inline-block;
            width: 28px;
            border: 1px solid #CCC;
            text-align: center;
            cursor: pointer;
            border-radius: 6px;
        }
        .variables
        {
            list-style: none;
        }
        .variable
        {
            border: 1px solid #CCC;
            list-style: none;
            border-radius: 6px;
            padding: 2px;
            margin: 2px;
        }
        .conflicted_value
        {
            border: solid 1px red;
        }
        .var_error_msg
        {
            color: Red;
            font-size: .75em;
        }
    </style>
</asp:Content>
<asp:Content ID="cDetail" ContentPlaceHolderID="phDetail" runat="server">
    <div class="embedded_step_header">
        <div class="step_header_title">
            Command Variables</div>
    </div>
    Task:
    <asp:Label ID="lblTaskName" runat="server"></asp:Label><br />
    Command:
    <asp:Label ID="lblCommandName" runat="server"></asp:Label>
    <asp:PlaceHolder ID="phVars" runat="server"></asp:PlaceHolder>
    <br />
    <input type="button" id="save_btn" value="Save Changes" />
    <input type="button" id="cancel_btn" value="Cancel" />
    <div class="hidden">
        <asp:HiddenField ID="hidStepID" runat="server" />
        <asp:HiddenField ID="hidOutputParseType" runat="server" />
        <asp:HiddenField ID="hidRowDelimiter" runat="server" />
        <asp:HiddenField ID="hidColDelimiter" runat="server" />
        <input type="hidden" id="delimiter_picker_target" />
    </div>
    <div id="delimiter_picker_dialog" title="Select a Delimiter">
        <ol id="ol_delimiters">
            <li id="d_9" class="delimiter" val="9">TAB</li>
            <li id="d_10" class="delimiter" val="10">LF</li>
            <li id="d_27" class="delimiter" val="27">ESC</li>
            <li id="d_32" class="delimiter" val="32">SP</li>
            <li id="d_33" class="delimiter" val="33">&#33;</li>
            <li id="d_34" class="delimiter" val="34">&#34;</li>
            <li id="d_35" class="delimiter" val="35">&#35;</li>
            <li id="d_36" class="delimiter" val="36">&#36;</li>
            <li id="d_37" class="delimiter" val="37">&#37;</li>
            <li id="d_38" class="delimiter" val="38">&#38;</li>
            <li id="d_39" class="delimiter" val="39">&#39;</li>
            <li id="d_40" class="delimiter" val="40">&#40;</li>
            <li id="d_41" class="delimiter" val="41">&#41;</li>
            <li id="d_42" class="delimiter" val="42">&#42;</li>
            <li id="d_43" class="delimiter" val="43">&#43;</li>
            <li id="d_44" class="delimiter" val="44">&#44;</li>
            <li id="d_45" class="delimiter" val="45">&#45;</li>
            <li id="d_46" class="delimiter" val="46">&#46;</li>
            <li id="d_47" class="delimiter" val="47">&#47;</li>
            <li id="d_48" class="delimiter" val="48">&#48;</li>
            <li id="d_49" class="delimiter" val="49">&#49;</li>
            <li id="d_50" class="delimiter" val="50">&#50;</li>
            <li id="d_51" class="delimiter" val="51">&#51;</li>
            <li id="d_52" class="delimiter" val="52">&#52;</li>
            <li id="d_53" class="delimiter" val="53">&#53;</li>
            <li id="d_54" class="delimiter" val="54">&#54;</li>
            <li id="d_55" class="delimiter" val="55">&#55;</li>
            <li id="d_56" class="delimiter" val="56">&#56;</li>
            <li id="d_57" class="delimiter" val="57">&#57;</li>
            <li id="d_58" class="delimiter" val="58">&#58;</li>
            <li id="d_59" class="delimiter" val="59">&#59;</li>
            <li id="d_60" class="delimiter" val="60">&#60;</li>
            <li id="d_61" class="delimiter" val="61">&#61;</li>
            <li id="d_62" class="delimiter" val="62">&#62;</li>
            <li id="d_63" class="delimiter" val="63">&#63;</li>
            <li id="d_64" class="delimiter" val="64">&#64;</li>
            <li id="d_65" class="delimiter" val="65">&#65;</li>
            <li id="d_66" class="delimiter" val="66">&#66;</li>
            <li id="d_67" class="delimiter" val="67">&#67;</li>
            <li id="d_68" class="delimiter" val="68">&#68;</li>
            <li id="d_69" class="delimiter" val="69">&#69;</li>
            <li id="d_70" class="delimiter" val="70">&#70;</li>
            <li id="d_71" class="delimiter" val="71">&#71;</li>
            <li id="d_72" class="delimiter" val="72">&#72;</li>
            <li id="d_73" class="delimiter" val="73">&#73;</li>
            <li id="d_74" class="delimiter" val="74">&#74;</li>
            <li id="d_75" class="delimiter" val="75">&#75;</li>
            <li id="d_76" class="delimiter" val="76">&#76;</li>
            <li id="d_77" class="delimiter" val="77">&#77;</li>
            <li id="d_78" class="delimiter" val="78">&#78;</li>
            <li id="d_79" class="delimiter" val="79">&#79;</li>
            <li id="d_80" class="delimiter" val="80">&#80;</li>
            <li id="d_81" class="delimiter" val="81">&#81;</li>
            <li id="d_82" class="delimiter" val="82">&#82;</li>
            <li id="d_83" class="delimiter" val="83">&#83;</li>
            <li id="d_84" class="delimiter" val="84">&#84;</li>
            <li id="d_85" class="delimiter" val="85">&#85;</li>
            <li id="d_86" class="delimiter" val="86">&#86;</li>
            <li id="d_87" class="delimiter" val="87">&#87;</li>
            <li id="d_88" class="delimiter" val="88">&#88;</li>
            <li id="d_89" class="delimiter" val="89">&#89;</li>
            <li id="d_90" class="delimiter" val="90">&#90;</li>
            <li id="d_91" class="delimiter" val="91">&#91;</li>
            <li id="d_92" class="delimiter" val="92">&#92;</li>
            <li id="d_93" class="delimiter" val="93">&#93;</li>
            <li id="d_94" class="delimiter" val="94">&#94;</li>
            <li id="d_95" class="delimiter" val="95">&#95;</li>
            <li id="d_96" class="delimiter" val="96">&#96;</li>
            <li id="d_97" class="delimiter" val="97">&#97;</li>
            <li id="d_98" class="delimiter" val="98">&#98;</li>
            <li id="d_99" class="delimiter" val="99">&#99;</li>
            <li id="d_100" class="delimiter" val="100">&#100;</li>
            <li id="d_101" class="delimiter" val="101">&#101;</li>
            <li id="d_102" class="delimiter" val="102">&#102;</li>
            <li id="d_103" class="delimiter" val="103">&#103;</li>
            <li id="d_104" class="delimiter" val="104">&#104;</li>
            <li id="d_105" class="delimiter" val="105">&#105;</li>
            <li id="d_106" class="delimiter" val="106">&#106;</li>
            <li id="d_107" class="delimiter" val="107">&#107;</li>
            <li id="d_108" class="delimiter" val="108">&#108;</li>
            <li id="d_109" class="delimiter" val="109">&#109;</li>
            <li id="d_110" class="delimiter" val="110">&#110;</li>
            <li id="d_111" class="delimiter" val="111">&#111;</li>
            <li id="d_112" class="delimiter" val="112">&#112;</li>
            <li id="d_113" class="delimiter" val="113">&#113;</li>
            <li id="d_114" class="delimiter" val="114">&#114;</li>
            <li id="d_115" class="delimiter" val="115">&#115;</li>
            <li id="d_116" class="delimiter" val="116">&#116;</li>
            <li id="d_117" class="delimiter" val="117">&#117;</li>
            <li id="d_118" class="delimiter" val="118">&#118;</li>
            <li id="d_119" class="delimiter" val="119">&#119;</li>
            <li id="d_120" class="delimiter" val="120">&#120;</li>
            <li id="d_121" class="delimiter" val="121">&#121;</li>
            <li id="d_122" class="delimiter" val="122">&#122;</li>
            <li id="d_123" class="delimiter" val="123">&#123;</li>
            <li id="d_124" class="delimiter" val="124">&#124;</li>
            <li id="d_125" class="delimiter" val="125">&#125;</li>
            <li id="d_126" class="delimiter" val="126">&#126;</li>
        </ol>
    </div>
    <div id="variable_add_dialog_delimited" title="Define Variable" class="ui-state-highlight">
        <table class="w100pct" border="0">
            <tr>
                <td width="40%">
                    Variable:
                </td>
                <td>
                    <input type="text" id="new_delimited_var_name" class="var_name w100pct code" />
                </td>
            </tr>
            <tr>
                <td>
                    Column Position:
                </td>
                <td>
                    <input type="text" id="new_delimited_position" class="w100pct code" />
                </td>
            </tr>
            <tr>
                <td align="center" colspan="2">
                    <span id="delimited_msg" class="var_error_msg"></span>
                </td>
            </tr>
        </table>
    </div>
    <div id="variable_add_dialog_parsed" title="Define Variable" class="ui-state-highlight">
        <table class="w100pct" border="0">
            <tr>
                <td colspan="3" align="center">
                    <input type="radio" name="parsed_type" value="delimited" />
                    Delimited&nbsp;
                    <input type="radio" name="parsed_type" value="range" checked="checked" />
                    Range&nbsp;
                    <input type="radio" name="parsed_type" value="regex" />
                    Regex&nbsp;
                    <input type="radio" name="parsed_type" value="xpath" />
                    Xpath&nbsp;
                    <hr />
                </td>
            </tr>
            <tr>
                <td width="40%">
                    Variable:
                </td>
                <td colspan="2">
                    <input type="text" id="new_parsed_var_name" class="var_name w100pct code" />
                </td>
            </tr>
            <tr>
                <td>
                    <span id="new_parsed_l_label">Begin Position:</span>
                </td>
                <td>
                    <div id="parsed_lmode_div">
                        <input type="radio" name="parsed_lmode_rb" value="index" checked="checked" />Position
                        <br />
                        <input type="radio" name="parsed_lmode_rb" value="string" />Prefix
                    </div>
                </td>
                <td valign="middle">
                    <input type="text" id="new_parsed_l_val" class="w100pct code" />
                </td>
            </tr>
            <tr>
                <td>
                    <span id="new_parsed_r_label">End Position:</span>
                </td>
                <td>
                    <div id="parsed_rmode_div">
                        <input type="radio" name="parsed_rmode_rb" value="index" checked="checked" />Position
                        <br />
                        <input type="radio" name="parsed_rmode_rb" value="string" />Suffix
                    </div>
                </td>
                <td valign="middle">
                    <input type="text" id="new_parsed_r_val" class="w100pct code" />
                </td>
            </tr>
            <tr>
                <td align="center" colspan="3">
                    <span id="parsed_msg" class="var_error_msg"></span>
                </td>
            </tr>
        </table>
    </div>
</asp:Content>
