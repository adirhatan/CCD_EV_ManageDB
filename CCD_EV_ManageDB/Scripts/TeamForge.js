//Add permissions from teamForge site
//see also View/ManageDB/teamForge.htm

function getForm(sourceDoc, formId) { return sourceDoc.getElementById(formId); }
function getFormElement(sourceForm, elementName) { return sourceForm[elementName]; for (var i = 0; i < sourceForm.elements.length; i++) { var theElement = sourceForm.elements[i]; if (theElement.name == elementName) { return theElement; } } }

function moveSelected(sourceElementId, targetElementId, moveAll) {
    var sourceElement = document.getElementById(sourceElementId); var targetElement = null; 
    if (targetElementId != null) { targetElement = document.getElementById(targetElementId); }
    if (sourceElement == null || sourceElement.tagName != "SELECT" || (targetElement != null && targetElement.tagName != "SELECT")) { return; }
    var unselectedOptions = new Array(); for (var i = 0; i < sourceElement.length; i++) {
        var option = sourceElement.options[i]; if (option.selected || moveAll) {
            if (targetElement != null) {
                var alreadyExists = false; for (var j = 0; j < targetElement.length; j++) { if (targetElement.options[j].value == option.value) { alreadyExists = true; break; } }
                if (!alreadyExists) { targetElement.options[targetElement.length] = new Option(option.text, option.value); } 
            } else { sourceElement.options[i] = null; i--; } 
        }
        option.selected = false;
    }
    updateHiddenForm();
}

function updateHiddenForm() { var sourceElement = document.getElementById('foundMultiple'); var theForm = sourceElement.form; var useridEl = theForm.elements['userids']; useridEl.value = ""; for (var i = 0; i < sourceElement.length; i++) { var option = sourceElement.options[i]; useridEl.value += option.value; if (i + 1 < sourceElement.length) { useridEl.value += ','; } } }
function clearSelect(formId, elementId) { var formElement = document.getElementById(formId).elements[elementId]; formElement.selectedIndex = -1; }