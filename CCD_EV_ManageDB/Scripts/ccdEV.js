var clone = 0;
function FormToStr(action,db) {
    // debugger;
    var x = document.getElementById("myForm");
    var txt = "";
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("_Query")) {
            txt = txt + x.elements[i].value + ";";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("Query").value = txt;

    txt = "";
    for (var i = 0; i < x.length; i++) {
        if ((x.elements[i].name.match("_Fields")) && (x.elements[i].value != "")){
            txt = txt + x.elements[i].value + ";";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("FieldsStr").value = txt;
    x.elements.namedItem("Skip").value = 0;
    if (action != undefined) {
        x.action = action;
        x.elements.namedItem("db").value = db;
    }
    x.submit();

}

//====
function AllFields() {
    var x = document.getElementById("myForm");
    // debugger;
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("_Fields"))  {
            x.elements[i].value="";
        }
    }
}

//====
function FormConfirmAction(fname,action,message) {
    //debugger;
    var r = confirm(message);
    if (r == true) {
        //var x = document.getElementById("tForm");
        var x = document.getElementById(fname);
        x.action = action;
        x.submit();
    }
}

function SessionsConfirmAction(fname, action, message, val_to_show,color) {
    //debugger;
    var x = document.getElementById(fname);
    $("#foundMultiple").css({ "background-color": color });
    var messageVal = x.elements.namedItem(val_to_show).value;
    var r = confirm(message + "\n" + messageVal);
    $("#foundMultiple").css({ "background-color": "white" });
    if (r == true) {
        //var x = document.getElementById(fname);
        x.action = action;
        x.submit();
    }
}

function ProjectsListConfirmAction(fname, action, message, action_val,table_val,proj_val) {
    //debugger;
    var x = document.getElementById(fname);
    var r = confirm(message + "\n" + x.elements.namedItem(action_val).value + " table-\"" + x.elements.namedItem(table_val).value + "\" of project-\"" + x.elements.namedItem(proj_val).value + "\"?");
    if (r == true) {
        x.action = action;
        x.submit();
    }
}

function fnClone(divClone,divInsert) {
    /* the 'true' possible value specifies to clone the childNodes as well.*/
    var oCloneNode = document.getElementById(divClone).cloneNode(true);
    document.getElementById(divInsert).insertBefore(oCloneNode,null);
    oCloneNode.style.display = "block";
    //oCloneNode.id = "some_id";
}
function fnEditSlideClone(divClone, divInsert,child) {
    /* the 'true' possible value specifies to clone the childNodes as well.*/
    var oCloneNode = document.getElementById(divClone).cloneNode(true);
    var child = document.createElement("input");
    child.setAttribute("type","button");
    child.setAttribute("value","add next slide field");
    child.setAttribute("class","button");
    child.setAttribute("onclick","fnCloneAdj('queryInput',this.parentNode)");
   // oCloneNode.appendChild(child);
    document.getElementById(divInsert).insertBefore(oCloneNode, null);
    oCloneNode.style.display = "block";
    //oCloneNode.id = "some_id";
}
function fnCloneAdj(divClone, referenceNode) {
    /* the 'true' possible value specifies to clone the childNodes as well.*/
    debugger;
    if ((divClone != "") && (divClone != undefined)) {
        var oCloneNode = document.getElementById(divClone).cloneNode(true);
        var sources = referenceNode.childNodes;
        var targets = oCloneNode.childNodes;
        var i;
        for (i = 0; i < 5; i++) {
            if ((targets[i].id != undefined) && (targets[i].id != "") && (targets[i].id == sources[i].id)) {
                targets[i].innerHTML = sources[i].innerHTML;
            }
        }
    } else {
        var oCloneNode = referenceNode.cloneNode(true);
    }
//    var child = document.createElement("input");
//    child.setAttribute("type", "button");
//    child.setAttribute("value", "add next slide field");
//    child.setAttribute("class", "button");
//    child.setAttribute("onclick", "fnCloneAdj('queryInput',this.parentNode)");
    //    oCloneNode.appendChild(child);

    referenceNode.parentNode.insertBefore(oCloneNode, referenceNode.nextSibling);
    oCloneNode.style.display = "block";
    //oCloneNode.id = "some_id";
}
function fnCloneAbove(divClone, referenceNode) {
    /* the 'true' possible value specifies to clone the childNodes as well.*/
    var oCloneNode = document.getElementById(divClone).cloneNode(true);
    referenceNode.insertBefore(oCloneNode, null);
    oCloneNode.style.display = "block";
    //oCloneNode.id = "some_id";
}
function removeElem(elem) {
    elem.parentNode.removeChild(elem);
    return false;
}
function changeLogExecCol(action,logDB) {
    //debugger;
    var form = document.getElementById("myForm");
    window.location = action + "?colname=" + encodeURIComponent(form.elements.namedItem("col").value) + "&logDB="+logDB;
}
function changeTMPCol(action) {
    //debugger;
    var form = document.getElementById("myForm");
    window.location = action + "?db=TMP&col=" + encodeURIComponent(form.elements.namedItem("col").value);
}
function changeRECCol(action) {
    //debugger;
    var form = document.getElementById("myForm");
    window.location = action + "?db=REC&col=" + encodeURIComponent(form.elements.namedItem("col").value);
}
function changePRMCol(action) {
    var form = document.getElementById("myForm");
    window.location = action + "?db=PRM&col=" + encodeURIComponent(form.elements.namedItem("col").value);
}
function changePRM_ArchCol(action) {
    var form = document.getElementById("myForm");
    window.location = action + "?db=PRM_Arch&col=" + encodeURIComponent(form.elements.namedItem("col").value);
}
function changeCOMPCol(action) {
    var form = document.getElementById("myForm");
    window.location = action + "?db=CMP&col=" + encodeURIComponent(form.elements.namedItem("col").value);
}
function changeCOMP_ArchCol(action) {
    var form = document.getElementById("myForm");
    window.location = action + "?db=CMP_Arch&col=" + encodeURIComponent(form.elements.namedItem("col").value);
}
function changeSkip() {
    var selectSkip = document.getElementById("_Skip");
    var x = document.getElementById("myForm");
    x.elements.namedItem("Skip").value = selectSkip.value;
    x.submit();
}
function sortField(field, direction, query) {
    //debugger;
    var x = document.getElementById("myForm");
    x.elements.namedItem("Query").value = query;
    x.elements.namedItem("SortStr").value = field;
    x.elements.namedItem("SortDirection").value = direction;
    x.elements.namedItem("Skip").value = 0;
    //x.elements.namedItem("jumpBottom").value = true;
    x.submit();
}
function QuickSort(field, direction, query) {
    //debugger;
    var x = document.getElementById("myForm");
    x.elements.namedItem("Query").value = query;
    x.elements.namedItem("SortStr").value = document.getElementById(field).value;
    x.elements.namedItem("SortDirection").value = document.getElementById(direction).value;
    x.elements.namedItem("Skip").value = 0;
    //x.elements.namedItem("jumpBottom").value = true;
    x.submit();
}

function QuerySessions(formName, action) {
    //debugger;
    var x = document.getElementById(formName);
    x.action = action;
    x.submit();
}

function SavedQueryAction(id, action) {
    //var selectSkip = document.getElementById("name");
    //window.location = action + "?Skip=" + selectSkip.value + "#pageBottom";
    window.location = action + "?_id=" + id;
}

function SaveQuery(action,db) {

    var URL = action;
    var posting = $.post(URL, { name: $('#SaveQName').val(),_db: db });

    posting.done(function (data) {
        $("#SaveQName").val(data);
    });
}

function RenameSavedQuery(_id,action) {

    //debugger;
    var URL = action;
    var SaveQName = "#SaveQName" + _id;
    var td = "#" + _id;
    var name = $(SaveQName).val();
    var posting = $.post(URL, { _id: _id ,name: name });

    posting.done(function (data) {
        $(td).html(data);
        //$(SaveQName).val() = "";
    });
}

$(document).ready(function () {
    $('#table_id').dataTable({
//        "deferRender": true,
        "iDisplayLength": -1,
        "bPaginate": true,
        "iCookieDuration": 60,
        "bStateSave": false,
        "bAutoWidth": true,
        "bScrollAutoCss": true,
        "bProcessing": true,
        "bRetrieve": true,
        "bSort": false,
        "bJQueryUI": true,
        //"sDom": '<"H"CTrf>t<"F"lip>',
        "sDom": '<"H"CTrif>t<"F"CTrif>',
        //"aLengthMenu": [[10, 20, 100, -1], [10, 20, 100, "All"]],
        "sScrollX": "100%",
        //"sScrollY": "700px",
        "sScrollY": (0.8 * $(window).height()),
        "bScrollCollapse": true,
        "fnInitComplete": function () {
            this.css("visibility", "visible");
        }
    });
});

var lastMLine;
function markLine(lineId) {
    //debugger;
    if (!(lastMLine === undefined)) {
        $(lastMLine).removeAttr('style');
    }
    var tmp = "#" + lineId;
    $(tmp).css({ "background-color": "yellow" });
    if (lastMLine == tmp) {
        $(lastMLine).removeAttr('style');
        lastMLine = 'undefined';
    } else {
        lastMLine = tmp;
    }
}

//function popup(url) {
//    newwindow = window.open(url, 'name', 'height=200,width=500,left=500,top=200');
//    if (window.focus) { newwindow.focus() }
//    return false;
//}


function ReportFormToStr(ImageSlide) {
    //debugger;
    var x = document.getElementById("myForm");
    x.elements.namedItem("ImageSlideID").value = ImageSlide;
    var txt = "";
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("_FieldsSlides")) {
            txt = txt + x.elements[i].value + ";";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("FieldsSlidesStr").value = txt;
    txt = "";
    for (var i = 0; i < x.length; i++) {
        if ((x.elements[i].name.match("_TitleItems")) && (x.elements[i].value != "")) {
            txt = txt + x.elements[i].value + ";";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("TitleItemsStr").value = txt;
    x.submit();
}
function ValidateResCompReportFormToStr() {   
    //debugger;
    var x = document.getElementById("myForm");
    var txt = "";
    for (var i = 0; i < x.length; i++) {
        if ( (x.elements[i].name.match("_L.*")) && (x.elements[i].checked) ) {
            txt = txt + x.elements[i].name.toString().replace("_L","") + ",";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("LanesStr").value = txt;
    var txt = "";
    for (var i = 0; i < x.length; i++) {
        if ((x.elements[i].name.match("_cb.*")) && (x.elements[i].checked)) {
            txt = txt + x.elements[i].name.toString().replace("_cb", "") + ",";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("FieldSlidesStr").value = txt;
    x.submit();
}
function ValidateResCompReportCheckAll() {
    //debugger;
    var x = document.getElementById("myForm");
    var c = document.getElementById("_checkAll");
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("_cb.*")) {
            x.elements[i].checked = c.checked;
        }
    }
}
function ValidateCheckAllGen(formId, _checkAll) {
    //debugger;
    var x = document.getElementById(formId);
    var c = document.getElementById(_checkAll);
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("_cb.*")) {
            x.elements[i].checked = c.checked;
        }
    }
}
function RefFormToStr() {
    //debugger;
    var x = document.getElementById("myForm");
    var txt = "";
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("_FieldsSlides")) {
            txt = txt + x.elements[i].value.replace(";","||") + ";";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("FieldsSlidesStr").value = txt;
    x.submit();
}

function PartialReportFormToStr() {
    //debugger;
    //bug - After using JS to uncheck and check again,values are duplicated. Sp parsing is done on server, validating all unchecked works from here
    var x = document.getElementById("myForm");
    var txt = "";
    for (var i = 0; i < x.length; i++) {
        if ((x.elements[i].name.match("_cb.*")) && (x.elements[i].checked)) {
            txt = txt + x.elements[i].name.toString().replace("_cb", "") + ",";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    if (txt == "") {
        alert("Please choose at least one test");
        return;
    }
    else {
        x.elements.namedItem("FieldSlidesStr").value = txt;
        x.submit();
    }
}


function IndicatorEditCopyLineAll() {
    //debugger;
    var x = document.getElementById("myForm");
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("w[1-9][0-9]*_.*"))
        {
            var str = x.elements[i].name;
            x.elements[i].value = document.getElementById(str.replace(/w\d+/,"w0")).value
        }
        if (x.elements[i].name.match("wb[1-9][0-9]*_.*")) {
            var str = x.elements[i].name;
            x.elements[i].checked = document.getElementById(str.replace(/wb\d+/, "wb0")).checked
        }
        if (x.elements[i].name.match("u[1-9][0-9]*")) {
            var str = x.elements[i].name;
            x.elements[i].value = document.getElementById(str.replace(/u\d+/, "u0")).value
        }
    }
}

function IndicatorEditCopyLineNextOne(linePrefix,nextLinePrefix) {
    //debugger;
    var x = document.getElementById("myForm");
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match(linePrefix + "\d*_.*")) {
            var str = x.elements[i].name;
            document.getElementById(str.replace(/w\d+/, nextLinePrefix)).value = x.elements[i].value;
        }
    }
}

function IndicatorsEditCheckBlocked(formId, checkId) {
    //debugger;
    var cbNamePrefix = checkId.substring(0, 4);
    var x = document.getElementById(formId);
    var c = document.getElementById(checkId);
    var sourceWeek = checkId.replace(/wb\d+_/, '');
    for (var i = 0; i < x.length; i++) {
        //wb1_0
        if (x.elements[i].name.match(cbNamePrefix + ".*"))
        {
            var targetWeek = x.elements[i].name.replace(/wb\d+_/, '');
            if (parseInt(targetWeek) > parseInt(sourceWeek))
            {
                x.elements[i].checked = c.checked;
            }
        }
    }
}

function ValidateCompReportCheckAll(formId, checkAllId, cbNamePrefix) {
    //debugger;
    var x = document.getElementById(formId);
    var c = document.getElementById(checkAllId);
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match(cbNamePrefix + ".*")) {
            x.elements[i].checked = c.checked;
        }
    }
}
//==================================================================================================================


function GenSubmitFormAction(formName, action) {
    //debugger;
    var x = document.getElementById(formName);
    x.action = action;
    x.submit();
}

function GenLocatioAction(action) {
    window.location = action ;
}
function GenLocatioAction_id(id, action) {
    window.location = action + "?_id=" + id;
}
function GenLocatioAction_id_newTab(id, action) {
    window.open(action + "?_id=" + id, '_blank').focus();
}
function GenLocatioAction_id_desc(id, action, descNAme) {
    //debugger;
    var dn = "#" + descNAme;
    var desc = $(dn).val();
    window.location = action + "?_id=" + id + "&desc=" + desc;
}

function GenLocatioAction_par(action, parName, parId) {
    var par = "#" + parId;
    var parVal = $(par).val();
    window.location = action + "&" + parName + "=" + parVal;
}

function GenLocatioConfirmAction(action,message) {
    var r = confirm(message);
    if (r == true) {
        window.location = action;
    }
}

function GenLocatioAction_id_ConfirmAction(id, action, message) {
    var r = confirm(message);
    if (r == true) {
        window.location = action + "?_id=" + id;
    }
}

//function GenAjaxSaveQuery(action, db) {

//    var URL = action;
//    var posting = $.post(URL, { name: $('#SaveQName').val(), _db: db });

//    posting.done(function (data) {
//        $("#SaveQName").val(data);
//    });
//}

function GenAjaxRenameElem_id(elem,_id, action) {

    //debugger;
    var URL = action;
    var SaveName = "#" + elem;
    var td = "#" + _id;
    var name = $(SaveName).val();
    var posting = $.post(URL, { _id: _id, name: name });

    posting.done(function (data) {
        $(td).html(data);
    });
}

function AjaxShowCompSummary(dialog, _id, NVM, IsArch,action) {

    //debugger;
    var URL = action;
    var td = "#" + dialog;
    if ($(td).dialog("isOpen")) $(td).dialog("close");
    $(td).dialog("option", "width", 380);
    $(td).dialog("option", "modal", true);
    $(td).html('<p>Now querying DB for:<br/>"' + _id + '" NVM revision-' + NVM + '. Please wait...</p><div id="progressbar"</div>');
    $(td).dialog("open");
    $("#progressbar").progressbar({
        value: false
    });

    var posting = $.post(URL, { _id: _id, NVM: NVM, IsArch: IsArch });
    posting.done(function (data) {
        if ($(td).dialog("isOpen")) $(td).dialog("close");
        $(td).html(data);
        $(td).dialog("open");
    });
}

function ShowHelpImgJS(title, imgName, win_width, win_length, img_width, _img_length) {

    //debugger;
    var myWindow = window.open("", "MsgWindow", "top=500,left=500,width=" + win_width + ",height=" + win_length);
    myWindow.document.write('<img src="/content/Images/'+ imgName + '" width="' + img_width + '" height="' + _img_length + 'px" alt="try"/>');
    myWindow.document.title = title;

}

