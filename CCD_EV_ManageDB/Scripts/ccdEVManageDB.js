var clone = 0;
function FormToStr() {
    //debugger;
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
    oCloneNode.appendChild(child);
    document.getElementById(divInsert).insertBefore(oCloneNode, null);
    oCloneNode.style.display = "block";
    //oCloneNode.id = "some_id";
}

function fnCloneAdj(divClone, referenceNode) {
    /* the 'true' possible value specifies to clone the childNodes as well.*/
    //debugger;
    var oCloneNode = document.getElementById(divClone).cloneNode(true);
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
function changeTMPCol(action) {
    //debugger;
    var form = document.getElementById("myForm");
    window.location = action + "?db=TMP&col=" + encodeURIComponent(form.elements.namedItem("col").value);
}
function changePRMCol(action) {
    var form = document.getElementById("myForm");
    window.location = action + "?db=PRM&col=" + encodeURIComponent(form.elements.namedItem("col").value);
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
        "sDom": 't<"F"CTrif>',
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

//$(document).ready(function () {
//    $('#queries').dataTable({
//        "iDisplayLength": -1,
//        "bPaginate": true,
//        "iCookieDuration": 60,
//        "bStateSave": false,
//        "bAutoWidth": true,
//        "bScrollAutoCss": true,
//        "bProcessing": true,
//        "bRetrieve": true,
//        "bSort": true,
//        "bJQueryUI": true,
//        //"sDom": '<"H"CTrf>t<"F"lip>',
//        "sDom": 't<"F"CTrif>',
//        //"aLengthMenu": [[10, 20, 100, -1], [10, 20, 100, "All"]],
//        "sScrollX": "100%",
//        //"sScrollY": "700px",
//        "sScrollY": (0.8 * $(window).height()),
//        "bScrollCollapse": true,
//        "fnInitComplete": function () {
//            this.css("visibility", "visible");
//        }
//    });
//});

$(document).ready(function () {
    //debugger;
    $('#queries').dataTable({
        "iDisplayLength": -1,
        //        "columns": [
        //            { "width": "2%" },
        //            { "width": "2%" },
        //            { "width": "5%" },
        //            { "width": "8%" },
        //            { "width": null },
        //            { "width": null },
        //            { "width": null },
        //            { "width": null }
        //          ],
          "columns": [
            { "orderable": false },
            { "orderable": false },
            { "orderable": true },
            { "orderable": false },
            { "orderable": true },
            { "orderable": true },
            { "orderable": false },
            { "orderable": false }
          ],
        "autoWidth": true,
        "jQueryUI": true,
        //        //"sDom": '<"H"CTrf>t<"F"lip>',
        "dom": 't<"F"CTrif>',
        "scrollX": "100%",
        //"sScrollY": "700px",
        "scrollY": (0.8 * $(window).height()),
        "scrollCollapse": true
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


function ReportFormToStr() {
    debugger;
    var x = document.getElementById("myForm");
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

function RefFormToStr() {
    //debugger;
    var x = document.getElementById("myForm");
    var txt = "";
    for (var i = 0; i < x.length; i++) {
        if (x.elements[i].name.match("_FieldsSlides")) {
            txt = txt + x.elements[i].value + ";";
        }
    }
    txt = txt.substr(0, txt.length - 1);
    //alert(txt);
    x.elements.namedItem("FieldsSlidesStr").value = txt;
    x.submit();

}


//==================================================================================================================


function GenSubmitFormAction(formName, action) {
    //debugger;
    var x = document.getElementById(formName);
    x.action = action;
    x.submit();
}

function GenLocatioAction(action) {
    window.location = action;
}
function GenLocatioAction_id(id, action) {
    window.location = action + "?_id=" + id;
}
function GenLocatioAction_id_desc(id, action, descNAme) {
    debugger;
    var dn = "#" + descNAme;
    var desc = $(dn).val();
    window.location = action + "?_id=" + id + "&desc=" + desc;
}

function GenLocatioAction_par(action, parName, parId) {
    var par = "#" + parId;
    var parVal = $(par).val();
    window.location = action + "&" + parName + "=" + parVal;
}

//function GenAjaxSaveQuery(action, db) {

//    var URL = action;
//    var posting = $.post(URL, { name: $('#SaveQName').val(), _db: db });

//    posting.done(function (data) {
//        $("#SaveQName").val(data);
//    });
//}

function GenAjaxRenameElem_id(elem, _id, action) {

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

function AjaxShowCompSummary(dialog, _id, NVM, IsArch, action) {

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