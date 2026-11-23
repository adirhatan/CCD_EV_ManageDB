/*
* SourceForge(r) Enterprise Edition
* Copyright 2007 CollabNet, Inc.  All rights reserved.
* http://www.collab.net
*/

// Style Guide Functions
function toggleDisplay(image, baseId) {
    var tableObject = document.getElementById(baseId);

    if (tableObject.style.display == 'table' || tableObject.style.display == 'block') {
        tableObject.style.display = 'none';
        if (image) {
            image.src = "/sf-images/icons/icon16_arrowcollapsed_gr.gif";
        }
    } else {
        if (document.all) {
            tableObject.style.display = 'block';
        } else {
            tableObject.style.display = 'table';
        }
        if (image) {
            image.src = "/sf-images/icons/icon16_arrowexpanded_gr.gif";
        }
    }
}


/*
* Gets a comma delimited list of selected values from a select box
*/
function getSelectedValues(sourceElement) {
    var valueString = "";

    if (sourceElement == null || sourceElement.tagName != "SELECT" || sourceElement.length == 0) {
        return;
    }

    for (var i = 0; i < sourceElement.length; i++) {
        var option = sourceElement.options[i];
        if (option.selected) {
            valueString += option.value + ",";
        }
    }

    return valueString.substring(0, valueString.length - 1);
}

// this is used to disaable the passed menu entry.
function disableMenuEntry(menuEntryId) {
    var menuEntry = document.getElementById(menuEntryId);
    menuEntry.innerHTML = '<div class="MenuItemDisabled">' + getDivText(menuEntry) + '</div>';
}


/*
* Get the text from a specific div tag.  This is used for button and menu disabling functionality
*/
function getDivText(entryDiv) {
    if (entryDiv == null) {
        return '';
    }

    if (entryDiv.childNodes.length == 0) {
        return '';
    }

    var entryText = '';
    for (var i = 0; i < entryDiv.childNodes.length; i++) {
        var divChild = entryDiv.childNodes[i];
        if (divChild.innerHTML != null) {
            entryText += divChild.innerHTML;
        }
    }

    if (entryText == '' && entryDiv.childNodes.length == 1 && entryDiv.childNodes[0].nodeName == '#text') {
        entryText = entryDiv.childNodes[0].nodeValue;
    }

    return entryText;
}

/*
*
* Functions for toggling buttons between the enabled and disabled states
*
*/
// This is used to store the old link information
var _buttonPreferences = new Array();
var _buttonInnerHTML = new Array();
var _buttonDelimiter = '::';

/*
* An object used for storing preferences pertaining to when a button should be enabled vs. disabled
*/
function ButtonPreference(buttonId, minSelected, maxSelected) {
    this.buttonId = buttonId;
    this.minSelected = minSelected;
    this.maxSelected = maxSelected;
    this.validationFunction = null;

    this.getButtonId = function () {
        return this.buttonId;
    };

    this.getMinSelected = function () {
        return this.minSelected;
    };

    this.getMaxSelected = function () {
        return this.maxSelected;
    };

    this.hasValidationFunction = function () {
        return this.validationFunction != null;
    };

    this.setValidationFunction = function (validationFunction) {
        this.validationFunction = validationFunction;
    };

    this.getValidationFunction = function () {
        return this.validationFunction;
    };
}

/*
* Registers a button with a form element so that depending on how many items are checked, the button status can be
*
*/
function registerButton(buttonId, formName, elementName, minSelected, maxSelected, validationFunction) {
    var prefId = formName + _buttonDelimiter + elementName;
    var prefArray = _buttonPreferences[prefId];

    if (prefArray == null) {
        prefArray = new Array();
        _buttonPreferences[prefId] = prefArray;
    }

    prefArray[prefArray.length] = new ButtonPreference(buttonId, minSelected, maxSelected);
    prefArray[prefArray.length - 1].setValidationFunction(validationFunction);

    var form = document.forms[formName];
    if (form != null) {
        var element = form.elements[elementName];
        /* If enablement is not based on the number of items selected, toggle buttons.
        * If enablement is based on the number of items selected and there is at least
        * one list element visible, toggle buttons.  Otherwise, disable the button. */
        if (element != null || (minSelected == -1 && maxSelected == -1)) {
            toggleButtons(element);
        } else {
            setButtonState(buttonId, "disabled");
        }
    }
}



function toggleButtonsCustomized(formElement) {
    if (formElement == null) {
        return;
    }

    var formName = '';
    var elementName = '';
    if (formElement.length != null && formElement.length > 0) {
        formName = formElement[0].form.name;
        elementName = formElement[0].name;
    } else {
        formName = formElement.form.name;
        elementName = formElement.name;
    }

    var prefId = formName + _buttonDelimiter + "_listItem";
    var prefList = _buttonPreferences[prefId];

    if (prefList == null) {
        return;
    }

    // Get the form and see how may checkboxes are checked
    var checked = 0;

    var checkboxes = document.forms[formName].elements[elementName];
    if (checkboxes.length == null) {
        if (checkboxes.checked) {
            checked = 1;
        }
    } else {
        for (var i = 0; i < checkboxes.length; i++) {
            if (checkboxes[i].checked) {
                checked++;
            }
        }
    }

    // Now step through the preferences and toggle anything that needs to be toggled
    for (var j = 0; j < prefList.length; j++) {
        var pref = prefList[j];
        var buttonState = "disabled";
        if (pref.hasValidationFunction()) {
            if (pref.getValidationFunction()(checkboxes)) {
                buttonState = "enabled";
            }
        } else if ((pref.getMinSelected() < 0 || checked >= pref.getMinSelected()) &&
            (pref.getMaxSelected() < 0 || checked <= pref.getMaxSelected())) {
            buttonState = "enabled";
        }

        // Set the button state
        setButtonState(pref.getButtonId(), buttonState);
    }
}



/*
* Toggles the state of any buttons that are listending to a specific form element
*/
function toggleButtons(formElement) {
    if (formElement == null) {
        return;
    }

    var formName = '';
    var elementName = '';
    if (formElement.length != null && formElement.length > 0) {
        formName = formElement[0].form.name;
        elementName = formElement[0].name;
    } else {
        formName = formElement.form.name;
        elementName = formElement.name;
    }

    toggleButtonsByName(formName, elementName);
}

/*
* Toggles the state of any buttons that are listening to a specific form name and element name
*/
function toggleButtonsByName(formName, elementName) {
    var prefId = formName + _buttonDelimiter + elementName;
    var prefList = _buttonPreferences[prefId];

    if (prefList == null) {
        return;
    }

    // Get the form and see how many checkboxes are checked
    // This is a bad hack that works with all versions of IE, do not modify it without testing this properly in the
    // planning folder artifact list in /sort/ and /rank/ mode!
    // The jQuery reads, look for the element that has the name /formName/ and within this element look for
    // "input" elements that are checked and have the name /elementName/. Count them and assign this to /checked/
    var checked = $j("*[name='" + formName + "'] input:checked[name='" + elementName + "']").length;


    // Now step through the preferences and toggle anything that needs to be toggled
    for (var j = 0; j < prefList.length; j++) {
        var pref = prefList[j];
        var buttonState = "disabled";
        if (pref.hasValidationFunction()) {
            var checkboxes = document.forms[formName].elements[elementName];
            if (pref.getValidationFunction()(checkboxes)) {
                buttonState = "enabled";
            }
        } else if ((pref.getMinSelected() < 0 || checked >= pref.getMinSelected()) &&
            (pref.getMaxSelected() < 0 || checked <= pref.getMaxSelected())) {
            buttonState = "enabled";
        }

        // Set the button state
        setButtonState(pref.getButtonId(), buttonState);
    }
}

/* Reset buttons in the specified form and enable a button with given buttonId.
Use this call only once in a page, as calling this twice will reset whatever
has been set in the first time. */
function toggleButtonsInsideIFrame(formElement, elementName0, buttonId) {
    if (formElement == null) {
        return;
    }

    var formName = '';
    var elementName = '';
    if (formElement.length != null && formElement.length > 0) {
        formName = formElement[0].form.name;
        if (elementName0 == null) {
            elementName = formElement[0].name;
        } else {
            elementName = elementName0;
        }
    } else {
        formName = formElement.form.name;
        elementName = formElement.name;
    }

    var prefId = formName + _buttonDelimiter + elementName;
    var prefList = _buttonPreferences[prefId];
    if (prefList == null) {
        return;
    }

    // Get the form and see how may checkboxes are checked
    var checked = 1;
    var checkboxes = document.forms[formName].elements[elementName];
    if (checkboxes.length == null) {
        if (checkboxes.checked) {
            checked = 1;
        }
    } else {
        for (var i = 0; i < checkboxes.length; i++) {
            if (checkboxes[i].checked) {
                checked++;
            }
        }
    }

    // Now step through the preferences and toggle anything that needs to be toggled
    for (var j = 0; j < prefList.length; j++) {
        var pref = prefList[j];
        var buttonState = "disabled";
        if (buttonId == undefined || pref.getButtonId() == buttonId) {
            if (pref.hasValidationFunction()) {
                if (pref.getValidationFunction()(checkboxes)) {
                    buttonState = "enabled";
                }
            } else if ((pref.getMinSelected() < 0 || checked >= pref.getMinSelected()) &&
		(pref.getMaxSelected() < 0 || checked <= pref.getMaxSelected())) {
                buttonState = "enabled";
            }
        }

        // Set the button state
        setButtonState(pref.getButtonId(), buttonState);
    }
}

function dissableOneButtonInsideIFrame(formElement, elementName0, buttonId) {
    if (formElement == null) {
        return;
    }

    var formName = '';
    var elementName = '';
    if (formElement.length != null && formElement.length > 0) {
        formName = formElement[0].form.name;
        if (elementName0 == null) {
            elementName = formElement[0].name;
        } else {
            elementName = elementName0;
        }
    } else {
        formName = formElement.form.name;
        elementName = formElement.name;
    }

    var prefId = formName + _buttonDelimiter + elementName;
    var prefList = _buttonPreferences[prefId];
    if (prefList == null) {
        return;
    }

    // Get the form and see how may checkboxes are checked
    var checked = 1;
    var checkboxes = document.forms[formName].elements[elementName];
    if (checkboxes.length == null) {
        if (checkboxes.checked) {
            checked = 1;
        }
    } else {
        for (var i = 0; i < checkboxes.length; i++) {
            if (checkboxes[i].checked) {
                checked++;
            }
        }
    }

    // Now step through the preferences and toggle anything that needs to be toggled
    for (var j = 0; j < prefList.length; j++) {
        var pref = prefList[j];
        var buttonState = "disabled";
        if (buttonId == undefined || pref.getButtonId() == buttonId) {
            setButtonState(pref.getButtonId(), buttonState);

        }

    }
}
/*
* Set the state of a button an sf-button in a form
*
*/
function setButtonStateWithElement(divTagElement, buttonId, state) {

    var isDropDown = false;
    var buttonLeft = null;
    var buttonMiddle = null;
    var buttonRight = null;

    if (state == 'disabled') {
        buttonLeft = getButtonSection(divTagElement, 'Left');
        buttonMiddle = getButtonSection(divTagElement, 'Middle');
        buttonRight = getButtonSection(divTagElement, 'Right');
    } else {
        buttonLeft = getButtonSection(divTagElement, 'LeftDisabled');
        buttonMiddle = getButtonSection(divTagElement, 'MiddleDisabled');
        buttonRight = getButtonSection(divTagElement, 'RightDisabled');
    }

    if (buttonRight == null) {
        buttonRight = getButtonSection(divTagElement, 'DropDownRight');
        isDropDown = true;
    }

    if (buttonLeft == null || buttonMiddle == null || buttonRight == null) {
        return false;
    }

    var buttonDisabled = false;
    if (buttonMiddle != null) {
        if (_buttonInnerHTML[buttonId] != null) {
            buttonMiddle.innerHTML = _buttonInnerHTML[buttonId];
            _buttonInnerHTML[buttonId] = null;
        } else {
            _buttonInnerHTML[buttonId] = buttonMiddle.innerHTML;
            buttonMiddle.innerHTML = getDivText(buttonMiddle);
            buttonDisabled = true;
        }
    }

    if (buttonDisabled) {
        buttonLeft.className = 'LeftDisabled';
        buttonMiddle.className = 'MiddleDisabled';

        if (!isDropDown) {
            buttonRight.className = 'RightDisabled';
        }
    } else {
        buttonLeft.className = 'Left';
        buttonMiddle.className = 'Middle';

        if (!isDropDown) {
            buttonRight.className = 'Right';
        }
    }

    return true;
}

/*
* Set the state of a button an sf-button in a form
*/
function setButtonState(buttonId, state) {
    var divTag = document.getElementById(buttonId);
    return setButtonStateWithElement(divTag, buttonId, state);
}

/*
* Get the div tag representing a specific portion of a button (left, middle, or right side)
*/
function getButtonSection(buttonContainer, buttonClass) {
    for (var i = 0; i < buttonContainer.childNodes.length; i++) {
        var buttonSection = buttonContainer.childNodes[i];

        if (buttonSection.className == buttonClass) {
            return buttonSection;
        }
    }

    return null;
}


// Beginning of menu functions

// Used to store menu list selection preferences
var _MenuListSelectPreferences = new Array();

/*
* An object used for storing preferences pertaining to when a button should be enabled vs. disabled
*/
function MenuPreference(menuEntryId, formName, elementName, minSelected, maxSelected) {
    this.menuEntryId = menuEntryId;
    this.minSelected = minSelected;
    this.maxSelected = maxSelected;
    this.formName = formName;
    this.elementName = elementName;
    this.originalContent = document.getElementById(menuEntryId).innerHTML;

    this.getFormName = function () {
        return this.formName;
    };

    this.getElementName = function () {
        return this.elementName;
    };

    this.getMinSelected = function () {
        return this.minSelected;
    };

    this.getMaxSelected = function () {
        return this.maxSelected;
    };

    this.getMenuEntryId = function () {
        return this.menuEntryId;
    };

    this.initialize = function () {
        var menuEntry = document.getElementById(this.menuEntryId);
        menuEntry.innerHTML = this.originalContent;

        if (!this.isEnabled()) {
            menuEntry.innerHTML = '<div class="MenuItemDisabled">' + getDivText(menuEntry) + '</div>';
        }
    };

    this.isEnabled = function () {
        if (this.minSelected <= 0 && this.maxSelected < 0) {
            return true;
        }

        var form = document.forms[this.formName];
        if (form == null) {
            return false;
        }

        // use jquery selector instead of "form.elements[<name>]", since the latter isn't uptodate for modified DOM
        var checkboxes = jQuery("input[name=" + this.elementName + "]").get();
        if (checkboxes == null) {
            return false;
        }

        var checked = 0;
        if (checkboxes.length == null) {
            if (checkboxes.checked) {
                checked = 1;
            }
        } else {
            for (var i = 0; i < checkboxes.length; i++) {
                if (checkboxes[i].checked) {
                    checked++;
                }
            }
        }

        return ((this.minSelected <= 0 || this.minSelected <= checked) &&
	    	(this.maxSelected < 0 || this.maxSelected >= checked));
    };
}

/*
* This method sets the selection preferences for a menu entry.  Based on a list select form, different
* numbers of checked items can change the behavior of menu options.
*/
function setMenuEntryPreference(menuId, entryId, formName, minSelected, maxSelected) {
    if (_MenuListSelectPreferences[menuId] == null) {
        _MenuListSelectPreferences[menuId] = new Array();
    }

    var prefId = _MenuListSelectPreferences[menuId].length;
    _MenuListSelectPreferences[menuId][prefId] = new MenuPreference(entryId, formName, '_listItem', minSelected, maxSelected);
}

/**
* Initialize the menu entries to the correct states
*/
function initMenu(menuId) {
    var preferences = _MenuListSelectPreferences[menuId];
    if (preferences != null) {
        for (var i = 0; i < preferences.length; i++) {
            preferences[i].initialize();
        }
    }
}

/*
* The new menu implementation
*/

/*
* First, check the browser
*/
var detect = navigator.userAgent.toLowerCase();
var thestring;

function Browser() {
    this.isKonqueror = false;
    this.isSafari = false;
    this.isOmniWebr = false;
    this.isOpera = false;
    this.isWebTV = false;
    this.isICab = false;
    this.isIE = false;
    this.isNS = false;
    this.isChrome = false;

    if (checkIt('konqueror')) this.isKonqueror = true;
    else if (checkIt('safari')) this.isSafari = true;
    else if (checkIt('omniweb')) this.isOmniWebr = true;
    else if (checkIt('opera')) this.isOpera = true;
    else if (checkIt('webtv')) this.isWebTV = true;
    else if (checkIt('icab')) this.isICab = true;
    else if (checkIt('msie')) this.isIE = true;
    else if (checkIt('chrome')) this.isChrome = true;
    //else if (!checkIt('compatible')) this.isNS = true;
    else this.isNS = true;
    return;
}

function checkIt(string) {
    var place = detect.indexOf(string) + 1;
    thestring = string;
    return place;
}

var browser = new Browser();

// Reference to the currently active button (none on open)
var activeButton = null;

// Capture mouse clicks on the page so any active button can be
// deactivated.

if (browser.isIE) {
    document.onmousedown = pageMousedown;
} else if (browser.isNS && document.addEventListener != null) {
    document.addEventListener("mousedown", pageMousedown, true);
}

function pageMousedown(event) {

    var el;

    // If there is no active menu, exit.

    if (!activeButton)
        return;

    // Find the element that was clicked on.

    if (browser.isIE)
        el = window.event.srcElement;
    if (browser.isNS)
        el = (event.target.className ? event.target : event.target.parentNode);

    // If the active button was clicked on, exit.

    if (el == activeButton)
        return;

    // If the element clicked on was not a menu button or item, close the
    // active menu.

    if (el.className != "menuButton" && el.className != "menuItem" &&
      el.className != "menuItemSep" && el.className != "menu" &&
      el.className != "AppButtonText" && el.className != "AppButtonPadding" && el.className != "AppButtonPaddingRight")
        closeMenu(activeButton);
}

function buttonClick(button, menuName) {
    // Associate the named menu to this button if not already done.

    if (!button.menu)
        button.menu = document.getElementById(menuName);

    // Reset the currently active button, if any.

    if (activeButton && activeButton != button)
        closeMenu(activeButton);

    // Toggle the button's state.

    if (button.isDepressed)
        closeMenu(button);
    else
        openMenu(button);

    return false;
}

function buttonMouseover(button, menuName) {

    // If any other button menu is active, deactivate it and activate this one.
    // Note: if this button has no menu, leave the active menu alone.

    if (activeButton && activeButton != button) {
        closeMenu(activeButton);
        if (menuName)
            buttonClick(button, menuName);
    }
}

function BrowserWidth() {
    var myWidth = 0;
    if (typeof (window.innerWidth) == 'number') {
        //Non-IE
        myWidth = window.innerWidth;
    } else {
        if (document.documentElement &&
        (document.documentElement.clientWidth || document.documentElement.clientHeight)) {
            //IE 6+ in 'standards compliant mode'
            myWidth = document.documentElement.clientWidth;
        } else {
            if (document.body && (document.body.clientWidth || document.body.clientHeight)) {
                //IE 4 compatible
                myWidth = document.body.clientWidth;
            }
        }
    }
    return myWidth;
}

function openMenu(button) {
    var w, dw, x, y, screenWidth, menuWidth;

    initMenu(button.menu.id);

    // For IE, set an explicit width on the first menu item. This will
    // cause link hovers to work on all the menu's items even when the
    // cursor is not over the link's text.

    if (browser.isIE && (button.menu.firstChild.style != null) && !button.menu.firstChild.style.width) {
        w = button.menu.firstChild.offsetWidth;
        button.menu.firstChild.style.width = w + "px";
        dw = button.menu.firstChild.offsetWidth - w;
        w -= dw;
        button.menu.firstChild.style.width = w + "px";
    }

    x = getPageOffsetLeft(button);
    y = getPageOffsetTop(button);
    var menuHeight = jQuery(button.menu).outerHeight();
    var buttonEl = jQuery(button);
    var windowEl = jQuery(window);

    if ((buttonEl.offset().top + buttonEl.height() + menuHeight) > (windowEl.height() + windowEl.scrollTop())) {
        // if we run out of space, put the menu above the button instead
        y = y - menuHeight;
    } else {
        // Position the associated drop down menu under the button and show it
        y = y + button.offsetHeight;
    }

    if (browser.isIE) {
        x += 2;
        y += 2;
    }
    if (browser.isNS && browser.version < 6.1)
        y--;
    else if (browser.isNS && button.menu.id != "filters") {
        var child = button.firstChild;
        // If we can get offsetLeft of child, that's likely
        // an image, which can be "float: right"'ed. Mozilla has
        // bug calculating parent link offsetLeft for euch images
        if (child != undefined && child.offsetLeft != undefined) {
            x += child.offsetLeft;
        }
    }

    // Determine the width of the menu
    if (browser.isIE) {
        screenWidth = window.screen.availWidth;
        menuWidth = button.menu.firstChild.offsetWidth;
    }
    else {
        screenWidth = screen.width;
        menuWidth = document.defaultView.getComputedStyle(button.menu, "").getPropertyValue("width");
        menuWidth = menuWidth.substr(0, menuWidth.length - 2);
    }

    var SEARCHES_MENU_WIDTH = 300;
    if (button.menu.id == "searches") {
        button.menu.style.width = SEARCHES_MENU_WIDTH;
        x = x - SEARCHES_MENU_WIDTH;
    }

    // With the width of the menu, correct the x position if necessary
    if (x > (BrowserWidth() - menuWidth - 5))
        x = x - menuWidth - 5;
    if (x < 0)
        x = 0;


    // Position and show the menu.

    button.menu.style.left = x + "px";
    button.menu.style.top = y + "px";
    button.menu.style.visibility = "visible";
    var blockFrameHeight

    //all the menu items size 25, except from the items in the number of results per page menu are 25 height
    var HEIGHT_COMMON_MENU_ITEM = 25
    var HEIGHT_TABLE_CONFIG_MENU_ITEM = 17
    var blockedRowHeight = HEIGHT_COMMON_MENU_ITEM
    if (button.menu.id == "configureTableMenu") {
        blockedRowHeight = HEIGHT_TABLE_CONFIG_MENU_ITEM
    }

    if (button.menu.id == "ProjectMenu") {
        var maxHeight = document.body.clientHeight;
        var minHeight = 50;
        //      blockFrameHeight = (document.getElementById('ProjectMenu').getElementsByTagName('a').length)*25

        if (maxHeight < button.menu.scrollHeight) {
            var height = Math.max(maxHeight - (y + 20), minHeight);

            if (button.menu.style.overflow != "auto") {
                button.menu.style.width = button.menu.offsetWidth + 15;
                button.menu.style.height = height + "px";
                button.menu.style.overflow = "auto";
            }
        } else {
            button.menu.style.overflow = "";
            button.menu.style.height = "auto";
        }
    }

    if (isIEVersion(6)) {
        if (!fixIframeObj) {
            fixIframeObj = createIEFix();
        }
        blockFrameHeight = (button.menu.getElementsByTagName('a').length) * blockedRowHeight
        fixIframeObj.style.display = '';
        fixIframeObj.style.width = menuWidth + 2
        fixIframeObj.style.top = y
        fixIframeObj.style.left = x
        fixIframeObj.style.height = blockFrameHeight

        if (button.menu.style.height != null && button.menu.style.height != '' && button.menu.style.height > 0) {
            fixIframeObj.style.height = button.menu.style.height;
        }
    }

    // Set button state and let the world know which button is
    // active.

    button.isDepressed = true;
    activeButton = button;
}

function closeMenu(button) {
    if (!button) return;
    if (button.menu)
        button.menu.style.visibility = "hidden";

    // Set button state and clear active menu global.
    if (isIEVersion(6)) {
        fixIframeObj = document.getElementById('blockIframeLayer')
        fixIframeObj.style.display = 'none'
    }
    button.isDepressed = false;
    activeButton = null;
}

/*
* Some help functions for the positioning
*/
function getPageOffsetLeft(el) {

    // Return the true x coordinate of an element relative to the page.

    return el.offsetLeft + (el.offsetParent ? getPageOffsetLeft(el.offsetParent) : 0);
}

function getPageOffsetTop(el) {

    // Return the true y coordinate of an element relative to the page.

    return el.offsetTop + (el.offsetParent ? getPageOffsetTop(el.offsetParent) : 0);
}

/*
* This method is used for toggling check boxes in a form.  All it needs is the form and
* the name of the checkbox elements being toggled.  The checkboxes always are populated to the state of the master
* checkbox.
*
* checker  - The actual element that is being used to toggle the checkboxes
* formName - The name of the form that is having elements toggled
* varName  - The name of the checkboxes that are being toggled by the master
*/
function toggleSelectItems(checker, formName, varName) {
    var form = eval("document." + formName);
    var checkBoxes = new Array();

    if (form == null) {
        return;
    }

    // Step through the form elements and see if we can find them manually
    for (var i = 0; i < form.elements.length; i++) {
        if (form.elements[i].name == varName) {
            checkBoxes.push(form.elements[i]);
        }
    }

    if (checkBoxes.length == 0) {
        return;
    }

    var numOptions = checkBoxes.length;
    if (numOptions == null) {
        checkBoxes.checked = checker.checked;
    } else {
        for (var i = 0; i < numOptions; i++) {
            checkBoxes[i].checked = checker.checked;
        }
    }

    toggleButtons(checkBoxes);
}

//for dhtml filter row hide / reveal
function trigger() {

    var filterrow = document.getElementById('filter');
    if (filterrow != undefined) {
        filterrow.style.display = "none";

        var ficon = document.getElementById('filtericon');
        ficon.src = ficon.src.replace('Hide', 'Show');

        var applyfilterbutton = document.getElementById('applyfilter');
        if (applyfilterbutton != undefined)
            applyfilterbutton.style.display = "none";

        var removefilterbutton = document.getElementById('removefilter');
        if (removefilterbutton != undefined)
            removefilterbutton.style.display = "none";

        var dividerbutton = document.getElementById('divider');
        if (dividerbutton != undefined)
            dividerbutton.style.display = "none";

        var applyfilterbutton2 = document.getElementById('applyfilter2');
        if (applyfilterbutton2 != undefined)
            applyfilterbutton2.style.display = "none";

        var removefilterbutton2 = document.getElementById('removefilter2');
        if (removefilterbutton2 != undefined)
            removefilterbutton2.style.display = "none";

        var dividerbutton2 = document.getElementById('divider2');
        if (dividerbutton2 != undefined)
            dividerbutton2.style.display = "none";

        var filterTD = document.getElementById('filterTD');
        if (filterTD != undefined) {
            filterTD.style.border = "1px solid #FFFFFF";
            filterTD.style.background = "#FFFFFF";
        }

    }
}

//for dhtml filter row hide / reveal when using Filters button
function trigger2(filterPlus) {

    var filterrow = document.getElementById('filter');
    if (filterrow != undefined) {
        filterrow.style.display = "none";

        var filterbutton = document.getElementById('filters_button');
        filterbutton.innerHTML = filterPlus;

        var filterbutton = document.getElementById('filters_button_img');
        filterbutton.innerHTML = "<img src=\"/sf-images/icons/table/icon_ShowFilters.gif\" width=\"16\" height=\"16\" alt=\"" + showFilterTxt + "\">";

        var applyfilterbutton = document.getElementById('applyfilter');
        if (applyfilterbutton != undefined)
            applyfilterbutton.style.display = "none";

        var removefilterbutton = document.getElementById('removefilter');
        if (removefilterbutton != undefined)
            removefilterbutton.style.display = "none";

        var dividerbutton = document.getElementById('divider');
        if (dividerbutton != undefined)
            dividerbutton.style.display = "none";

        var applyfilterbutton2 = document.getElementById('applyfilter2');
        if (applyfilterbutton2 != undefined)
            applyfilterbutton2.style.display = "none";

        var removefilterbutton2 = document.getElementById('removefilter2');
        if (removefilterbutton2 != undefined)
            removefilterbutton2.style.display = "none";

        var dividerbutton2 = document.getElementById('divider2');
        if (dividerbutton2 != undefined)
            dividerbutton2.style.display = "none";

        var filterTD = document.getElementById('filterTD');
        if (filterTD != undefined) {
            filterTD.style.border = "1px solid #FFFFFF";
            filterTD.style.background = "#FFFFFF";
        }
    }
}

function collapseRow() {

    if (document.getElementById('filter').style.display == "") {
        trigger();
    }
    else {
        var filterrow = document.getElementById('filter');
        filterrow.style.display = "";

        var ficon = document.getElementById('filtericon');
        ficon.src = ficon.src.replace('Show', 'Hide');

        var applyfilterbutton = document.getElementById('applyfilter');
        if (applyfilterbutton != undefined)
            applyfilterbutton.style.display = "";

        var removefilterbutton = document.getElementById('removefilter');
        if (removefilterbutton != undefined)
            removefilterbutton.style.display = "";

        var dividerbutton = document.getElementById('divider');
        if (dividerbutton != undefined)
            dividerbutton.style.display = "";

        var applyfilterbutton2 = document.getElementById('applyfilter2');
        if (applyfilterbutton2 != undefined)
            applyfilterbutton2.style.display = "";

        var removefilterbutton2 = document.getElementById('removefilter2');
        if (removefilterbutton2 != undefined)
            removefilterbutton2.style.display = "";

        var dividerbutton2 = document.getElementById('divider2');
        if (dividerbutton2 != undefined)
            dividerbutton2.style.display = "";

        var filterTD = document.getElementById('filterTD');
        if (filterTD != undefined) {
            filterTD.style.border = "1px solid #CCCCCC";
            filterTD.style.background = "#FAFAFA";
        }
    }
}
// this collapse is used by Filters Button in the artifacts_list_tile.jsp
function collapseRow2(filterPlus, filterMinus) {
    if (document.getElementById('filter').style.display == "") {
        trigger2(filterPlus);
    } else {
        var filterrow = document.getElementById('filter');
        filterrow.style.display = "";

        var filterbutton = document.getElementById('filters_button');
        filterbutton.innerHTML = filterMinus;

        var filterbutton = document.getElementById('filters_button_img');
        filterbutton.innerHTML = "<img src=\"/sf-images/icons/table/icon_HideFilters.gif\" width=\"16\" height=\"16\" alt=\"" + hideFilterTxt + " \">";

        var applyfilterbutton = document.getElementById('applyfilter');
        if (applyfilterbutton != undefined)
            applyfilterbutton.style.display = "";

        var removefilterbutton = document.getElementById('removefilter');
        if (removefilterbutton != undefined)
            removefilterbutton.style.display = "";

        var dividerbutton = document.getElementById('divider');
        if (dividerbutton != undefined)
            dividerbutton.style.display = "";

        var applyfilterbutton2 = document.getElementById('applyfilter2');
        if (applyfilterbutton2 != undefined)
            applyfilterbutton2.style.display = "";

        var removefilterbutton2 = document.getElementById('removefilter2');
        if (removefilterbutton2 != undefined)
            removefilterbutton2.style.display = "";

        var dividerbutton2 = document.getElementById('divider2');
        if (dividerbutton2 != undefined)
            dividerbutton2.style.display = "";

        var filterTD = document.getElementById('filterTD');
        if (filterTD != undefined) {
            filterTD.style.border = "1px solid #CCCCCC";
            filterTD.style.background = "#FAFAFA";
        }

        if (typeof hideShowResize == 'function') {
            hideShowResize(4, 5);
        }
    }
}

//drop down menu jump script
function MM_jumpMenu(targ, selObj, restore) { //v3.0
    eval(targ + ".location='" + selObj.options[selObj.selectedIndex].value + "'");
    if (restore) selObj.selectedIndex = 0;
}



//auto centering popup code...
var win = null;
function NewWindow(mypage, myname, w, h, scroll) {
    var leftPosition = (screen.width) ? (screen.width - w) / 2 : 0;
    var topPosition = (screen.height) ? (screen.height - h) / 2 : 0;
    var settings = 'height=' + h +
             ',width=' + w +
             ',top=' + topPosition +
             ',left=' + leftPosition +
             ',scrollbars=' + scroll +
             ',resizable=yes';
    win = window.open(mypage, myname, settings);
    if (win != null) {
        win.focus();
    }
}

// The code in this file is not optimized for titan production... keep that in mind when using for development purposes.

//dhtml tabs

function domTab(i, n) {
    // Variables for customisation:
    var numberOfTabs = 5;
    var colourOfInactiveTab = "#D2D2D2";
    var colourOfActiveTab = "#DFDFDF";
    var colourOfInactiveLink = "#333333";
    var colourOfActiveLink = "#333333";
    var colourOfActiveBottomBorder = "#DFDFDF";
    var colourOfInactiveBottomBorder = "1px solid #ff0000";
    // end variables
    if (document.getElementById) {
        for (var f = 1; f < numberOfTabs + 1; f++) {
            document.getElementById('contentblock' + f).style.display = 'none';
            document.getElementById('link' + f).style.background = colourOfInactiveTab;
            document.getElementById('link' + f).style.color = colourOfInactiveLink;
        }
        document.getElementById('contentblock' + i).style.display = 'block';
        document.getElementById('link' + i).style.background = colourOfActiveTab;

        document.getElementById('link' + i).style.color = colourOfActiveLink;
    }
}




//for the open / close functions in the leftnav
function toggleOpenMenu(o, framesetCall) {
    var i, l, t;
    i = o.getElementsByTagName('img')[0];
    l = o.parentNode.getElementsByTagName('div')[1];
    if (l.style.display == 'block') {
        l.style.display = 'none';
        i.src = i.src.replace('_open.', '_closed.');
    } else {
        i.src = i.src.replace('_closed.', '_open.');
        l.style.display = 'block';
    }
}

function toggleCloseMenu(o, openstate, framesetCall) {
    var i, l, t;
    i = o.getElementsByTagName('img')[0];
    l = o.parentNode.getElementsByTagName('div')[1];

    if (l.style.display != 'none') {
        l.style.display = 'none';
        i.src = i.src.replace('_open.', '_closed.');
    }
    else {
        l.style.display = 'block';
        i.src = i.src.replace('_closed.', '_open.');
    }
}

// No need to edit beyond here

// --- START MENU CODE ---
var ie4 = document.all && (navigator.userAgent.indexOf("Opera") == -1);
var ns6 = document.getElementById && !document.all;
var ns4 = document.layers;

function showmenu(e, which) {
    if (!document.all && !document.getElementById && !document.layers) {
        return;
    }

    clearhidemenu();
    var menuobj = ie4 ? document.all.popmenu : ns6 ? document.getElementById("popmenu") : ns4 ? document.popmenu : "";
    menuobj.thestyle = (ie4 || ns6) ? menuobj.style : menuobj;
    if (ie4 || ns6) {
        menuobj.innerHTML = which;
    } else {
        menuobj.document.write('<layer name=gui bgColor=#E6E6E6 width=165 onmouseover="clearhidemenu()" onmouseout="hidemenu()">' + which + '</layer>');
        menuobj.document.close();
    }

    menuobj.contentwidth = (ie4 || ns6) ? menuobj.offsetWidth : menuobj.document.gui.document.width;
    menuobj.contentheight = (ie4 || ns6) ? menuobj.offsetHeight : menuobj.document.gui.document.height;

    var eventX = ie4 ? event.clientX : ns6 ? e.clientX : e.x;
    var eventY = ie4 ? event.clientY : ns6 ? e.clientY : e.y;

    //Find out how close the mouse is to the corner of the window
    var rightedge = ie4 ? document.body.clientWidth - eventX : window.innerWidth - eventX;
    var bottomedge = ie4 ? document.body.clientHeight - eventY : window.innerHeight - eventY;

    //if the horizontal distance isn't enough to accomodate the width of the context menu
    if (rightedge < menuobj.contentwidth) {
        //move the horizontal position of the menu to the left by it's width
        menuobj.thestyle.left = ie4 ? document.body.scrollLeft + eventX - menuobj.contentwidth : ns6 ? window.pageXOffset + eventX - menuobj.contentwidth : eventX - menuobj.contentwidth;
    } else {
        //position the horizontal position of the menu where the mouse was clicked
        menuobj.thestyle.left = ie4 ? document.body.scrollLeft + eventX : ns6 ? window.pageXOffset + eventX : eventX;
    }

    //same concept with the vertical position
    if (bottomedge < menuobj.contentheight) {
        menuobj.thestyle.top = ie4 ? document.body.scrollTop + eventY - menuobj.contentheight : ns6 ? window.pageYOffset + eventY - menuobj.contentheight : eventY - menuobj.contentheight;
    } else {
        menuobj.thestyle.top = ie4 ? document.body.scrollTop + event.clientY : ns6 ? window.pageYOffset + eventY : eventY;
    }

    menuobj.thestyle.visibility = "visible";

    return false;
}

function contains_ns6(a, b) {
    if (b == null) {
        return true;
    }

    //Determines if 1 element in contained in another- by Brainjar.com
    while (b.parentNode) {
        if ((b = b.parentNode) == a) {
            return true;
        }
    }

    return false;
}

function hidemenu() {
    if (window.menuobj) {
        menuobj.thestyle.visibility = (ie4 || ns6) ? "hidden" : "hide";
    }
}

function dynamichide(e) {
    if (ie4 && !menuobj.contains(e.toElement)) {
        hidemenu();
    } else if (ns6 && e.currentTarget != e.relatedTarget && !contains_ns6(e.currentTarget, e.relatedTarget)) {
        hidemenu();
    }
}

function delayhidemenu() {
    if (ie4 || ns6 || ns4) {
        delayhide = setTimeout("hidemenu()", 500);
    }
}

function clearhidemenu() {
    if (window.delayhide) {
        clearTimeout(delayhide);
    }
}

function highlightmenu(e, state) {
    if (document.all) {
        source_el = event.srcElement;
    } else if (document.getElementById) {
        source_el = e.target;
    }

    if (source_el.className == "menuitems") {
        source_el.id = (state == "on") ? "menumouseover" : "";
    } else {
        while (source_el.id != "popmenu") {
            source_el = document.getElementById ? source_el.parentNode : source_el.parentElement;
            if (source_el.className == "menuitems") {
                source_el.id = (state == "on") ? "menumouseover" : "";
            }
        }
    }
}

// --- END MENU CODE ---

/**
* Set the action of a form to a new page
*/
function setFormAction(formName, formAction) {
    var theForm = eval('document.' + formName);
    theForm.action = formAction;

    return true;
}

/**
* Submit a form with a given name.  The value of the 'sfsubmit' variable is set with the second parameter.
*/
function submitForm(form, submitValue) {
    // This is used to match the button
    if (form.name == "selectForumsForm" && submitValue == "Monitor") {
        monitorTypeModalPopup();
    } else {
        if (form.name == "myProjects" && submitValue == "delete") {
            form.action = "/sf/sfmain/do/listProjectGroupsAdmin";
            form.name = "listProjectGroupsAdmin";
        }
        var linkCall = "submitForm(";

        // At this point, we want to turn off this submit button
        for (var i = 0; i < document.links.length; i++) {
            var link = document.links[i];
            if (link.href.indexOf(linkCall) > 0) {
                link.href = 'javascript:void(0);';
            }
        }

        form.sfsubmit.value = submitValue;
        form.submit();
    }
} // submitForm

/**
* Submit a form with a given name.  The value of the 'sfsubmit' variable is set with the second parameter.
* submit the form without voiding the button
*/
function submitFormNoVoidButtons(form, submitValue) {
    form.sfsubmit.value = submitValue;
    form.submit();
} // submitForm



function trim(inputString) {
    if (typeof inputString != "string") { return inputString; }
    var retValue = inputString;
    var ch = retValue.substring(0, 1);
    while (ch == " ") { // Check for spaces at the beginning of the string
        retValue = retValue.substring(1, retValue.length);
        ch = retValue.substring(0, 1);
    }
    ch = retValue.substring(retValue.length - 1, retValue.length);
    while (ch == " ") { // Check for spaces at the end of the string
        retValue = retValue.substring(0, retValue.length - 1);
        ch = retValue.substring(retValue.length - 1, retValue.length);
    }
    return retValue; // Return the trimmed string back to the user
} // Ends the "trim" function

//function to trigger default action onclick of enter key.
function submitDefault(evt, form, submitValue, validate) {

    var e;
    if (ie4) {
        e = window.event;
    } else if (ns6 || ns4) {
        e = evt;
    }

    if (e && e.keyCode == 13) {
        if (validate == "true") {
            var formName = form.name;
            var methodName = "validate" + formName.charAt(0).toUpperCase() + formName.substring(1, formName.length);
            var functionName = methodName + "(" + form.name + ")";
            if (eval(functionName)) {
                submitForm(form, submitValue);
            }
        } else {
            submitForm(form, submitValue);
        }
    }
} //End of submitDefault

//function to prevent default action onclick of enter key.
function disableOnEnterSubmit(evt) {

    var e;
    if (ie4) {
        e = window.event;
    } else if (ns6 || ns4) {
        e = evt;
    }

    return !(e && e.keyCode == 13);
} //End of noSubmitDefault

// function to submit a login form on click of the 'enter' key.
// When the enter key is clicked, we submit the form named "login"
function submitLoginOnEnter(evt) {
    if (ie4) {
        if (window.event && window.event.keyCode == 13) {
            submitForm(document.forms['login'], 'submit');
        }
    } else if (ns6 || ns4) {
        if (evt && evt.which == 13) {
            submitForm(document.forms['login'], 'submit');
        }
    }
    // else: unknown browser, so we do nothing.
} //End of submitOnEnter

// Change the count number in a tab
function showCount(elementId, count) {
    var pageElement = document.getElementById(elementId);
    if (pageElement != null) {
        pageElement.innerHTML = count;
    }
}

//Functions for moving options from one select list to another list

function moveOneRight(lstFirst, lstSecond, noSelect) {
    for (var i = 0; i < lstFirst.options.length; ) {
        if (lstFirst.options[i].selected) {
            var opt = new Option(lstFirst.options[i].text, lstFirst.options[i].value);
            lstFirst.options[i] = null;
            lstSecond.options[lstSecond.options.length] = opt;
            opt.selected = true;
        } else {
            i++;
        }
    }

    if (lstSecond.options.length > 1 && lstSecond.options[0].value == "") {
        lstSecond.options[0] = null;
    }

    sortList(lstFirst, false, false);
    if (!noSelect) selectRight(lstSecond);
}

function moveAllRight(lstFirst, lstSecond, noSelect) {
    for (var i = 0; i < lstFirst.options.length; ) {
        var opt = new Option(lstFirst.options[i].text, lstFirst.options[i].value);
        lstSecond.options[lstSecond.options.length] = opt;
        lstFirst.options[i] = null;
    }

    if (lstSecond.options.length > 1 && lstSecond.options[0].value == "") {
        lstSecond.options[0] = null;
    }

    sortList(lstFirst, false, false);
    if (!noSelect) selectRight(lstSecond);
}

function moveOneLeft(lstFirst, lstSecond) {
    if (lstSecond.options.length == 1 && lstSecond.options[0].value == "") {
        lstSecond.options[0].selected = false;
        return;
    }

    for (var i = 0; i < lstSecond.options.length; ) {
        if (lstSecond.options[i].selected) {
            var opt = new Option(lstSecond.options[i].text, lstSecond.options[i].value);
            lstFirst.options[lstFirst.options.length] = opt;
            lstSecond.options[i] = null;
        } else {
            i++;
        }
    }

    sortList(lstFirst, false, false);

    selectRight(lstSecond);
}

function moveAllLeft(lstFirst, lstSecond) {
    if (lstSecond.options.length == 1 && lstSecond.options[0].value == "") {
        lstSecond.options[0].selected = false;
        return;
    }

    for (var i = 0; i < lstSecond.options.length; ) {
        var opt = new Option(lstSecond.options[i].text, lstSecond.options[i].value);
        lstFirst.options[lstFirst.options.length] = opt;
        lstSecond.options[i] = null;
    }
    sortList(lstFirst, false, false);

    selectRight(lstSecond);
}

function selectRight(lstSecond) {
    for (var i = 0; i < lstSecond.options.length; i++) {
        lstSecond.options[i].selected = true;
    }
}

// Compare functions used internally by the SortList routine,
function ListCompareNums(a, b) {
    var la = parseInt(a.split("{")[0]);
    var lb = parseInt(b.split("{")[0]);
    if (la < lb) return -1;
    if (la > lb) return 1
    return 0;
}
function ListCompareText(a, b) {
    var la = a.toLowerCase();
    var lb = b.toLowerCase();
    if (la < lb) return -1;
    if (la > lb) return 1
    return 0;
}

// Quite Fast way of sorting big option lists
function sortList(Combo, IntSort, UseVals) {
    var cmbText = new Array(Combo.options.length);
    var cmbItems;

    // get copy of
    for (x = 0; x < cmbText.length; x++)
        cmbText[x] = (UseVals) ? Combo.options[x].value + "{" + Combo.options[x].text : Combo.options[x].text + "{" + Combo.options[x].value;

    if (IntSort)
        cmbText.sort(ListCompareNums);
    else
        cmbText.sort(ListCompareText);

    //rebuild main list, but don't redimension it
    for (x = 0; x < cmbText.length; x++) {
        cmbItems = cmbText[x].split("{");
        Combo.options[x].text = (UseVals) ? cmbItems[1] : cmbItems[0];
        Combo.options[x].value = (UseVals) ? cmbItems[0] : cmbItems[1];
    }
}

//end of field picker functions.


//used by artifacts filter to capture the enter key on the text input fields
function captureEnterAndSubmit(e, formName, formAction, form, submitValue) {

    if (!e) {
        if (window.event) {
            //Internet Explorer
            e = window.event;
        } else {
            return;
        }
    }
    if (typeof (e.keyCode) == 'number') {
        //DOM
        e = e.keyCode;
    } else if (typeof (e.which) == 'number') {
        //NS 4 compatible
        e = e.which;
    } else if (typeof (e.charCode) == 'number') {
        //also NS 6+, Mozilla 0.9+
        e = e.charCode;
    } else {
        //total failure, we have no way of obtaining the key code
        //nothing is made
        return;
    }
    if (e == 13) {
        setFormAction(formName, formAction) && submitForm(form, submitValue);
    }
}

function changeVisibility(state) {
    var v = document.getElementById("length_error_title_span");
    if (state == "view") {
        v.style.display = "block";
    }
    else {
        if (state == "hide") {
            v.style.display = "none";
        }
    }
}

function textCounter(field, maxlimit) {
    if (field.value.length >= maxlimit) {
        changeVisibility("view");
    }
    else {
        changeVisibility("hide");
    }
}

//detect if we are using IE

function isIEVersion(version) {
    localVersion = 0
    if (navigator.appVersion != null && navigator.appVersion.indexOf("MSIE") != -1) {
        temp = navigator.appVersion.split("MSIE")
        localVersion = parseFloat(temp[1])
    }

    if (localVersion == version) {
        return true
    }
    return false
}

var fixIframeObj;

function createIEFix() {
    var fixIframeObj;
    if (isIEVersion(6)) {
        fixIframeObj = document.createElement('IFRAME');
        fixIframeObj.setAttribute('src', '.');
        fixIframeObj.id = 'blockIframeLayer'
        fixIframeObj.style.filter = 'alpha(opacity=0)';
        fixIframeObj.style.position = 'absolute';
        fixIframeObj.style.zIndex = '0';
        document.body.appendChild(fixIframeObj);
    }
    return fixIframeObj;

}

// pce: start
function page_addcomponent(pageFolderPath, componentType) {
    this.location = "/sf/page/do/createComponent/" + pageFolderPath + "?component_type=" + componentType;
}

// variables used for reordering components
var numberOfComponents;
var slotMatrix = new Array();

// set the number of components that will be on the page
function setNumberOfComponents(count) {
    numberOfComponents = count;
}

// put a component id at a new display position 
function updatePageComponentMatrix(position, componentId) {
    slotMatrix[position] = componentId;
}


function getObject(id) {
    if (ie4) {
        return document.all[id];
    } else {
        return document.getElementById(id);
    }
}

// swap the position of 2 page components in the browser.
// this is called after the ajax processes
function processMove(oldIndex, newIndex) {

    // update the html
    var newslot = getObject("componentslot_" + (newIndex));
    var oldslot = getObject("componentslot_" + (oldIndex));
    var tempslot = newslot.innerHTML;
    newslot.innerHTML = oldslot.innerHTML;
    oldslot.innerHTML = tempslot;

    //update the array
    var tempId = slotMatrix[newIndex];
    slotMatrix[newIndex] = slotMatrix[oldIndex];
    slotMatrix[oldIndex] = tempId;

    // fix arrow icons
    var newslot_up_arrow = getObject("moveup_" + slotMatrix[newIndex]);
    var newslot_down_arrow = getObject("movedown_" + slotMatrix[newIndex]);
    var oldslot_up_arrow = getObject("moveup_" + slotMatrix[oldIndex]);
    var oldslot_down_arrow = getObject("movedown_" + slotMatrix[oldIndex]);

    newslot_up_arrow.className = "uparrow";
    oldslot_up_arrow.className = "uparrow";
    if (newIndex <= 0)
        newslot_up_arrow.className = "uparrowgray";
    if (oldIndex <= 0)
        oldslot_up_arrow.className = "uparrowgray";

    newslot_down_arrow.className = "downarrow";
    oldslot_down_arrow.className = "downarrow";
    if (newIndex >= numberOfComponents - 1)
        newslot_down_arrow.className = "downarrowgray";
    if (oldIndex >= numberOfComponents - 1)
        oldslot_down_arrow.className = "downarrowgray";
}

// move a component up 
function moveUp(comp_id, pageFolderPath, msg) {
    for (i = 0; i < slotMatrix.length; i++) {
        if (comp_id == (slotMatrix[i])) {
            if (i > 0) {
                reorderPageComponents(pageFolderPath, i, 'up', msg);
                return;
            }
        }
    }
}

// move a component down
function moveDown(comp_id, pageFolderPath, msg) {
    for (i = 0; i < slotMatrix.length; i++) {
        if (comp_id == (slotMatrix[i])) {
            if (i < numberOfComponents - 1) {
                reorderPageComponents(pageFolderPath, i, 'down', msg);
                return;
            }
        }
    }
}

// process component reorder through ajax
function reorderPageComponents(pageFolderPath, compIndex, direction, msg) {
    var compId = slotMatrix[compIndex];
    // remove the component_ prefix from compId
    compId = compId.substring(10, compId.length);
    var newIndex = (direction == "up") ? compIndex-- : compIndex++;
    var url = "/sf/page/do/reorderComponent/" + pageFolderPath + "/" + compId + "?dir=" + direction;
    var http = window.XMLHttpRequest ? new XMLHttpRequest() :
        new ActiveXObject("Microsoft.XMLHTTP");
    http.open('get', url);
    http.onreadystatechange = function () {
        if (http.readyState == 4) {
            var response = http.responseText;
            if (response.match("success")) {
                processMove(compIndex, newIndex);
            } else {
                alert(msg);
            }
        }
    };
    http.send(null);
    return false;
}

// process edit component through ajax
// currently not used
function editComponent(pageFolderPath, componentId, formId) {
    // TODO: will need to escape text values.
    var obj = document.getElementById(formId);
    var getstr = "?";
    var newTitle = "";
    for (i = 0; i < obj.childNodes.length; i++) {
        if (obj.childNodes[i].tagName == "INPUT") {
            if (obj.childNodes[i].type == "text") {
                getstr += obj.childNodes[i].name + "=" + obj.childNodes[i].value + "&";
                //TODO: this is just grabbing any text field needs to be verify this is the title field!
                newTitle = obj.childNodes[i].value;
            }
            if (obj.childNodes[i].type == "checkbox") {
                if (obj.childNodes[i].checked) {
                    getstr += obj.childNodes[i].name + "=" + obj.childNodes[i].value + "&";
                } else {
                    getstr += obj.childNodes[i].name + "=&";
                }
            }
            if (obj.childNodes[i].type == "radio") {
                if (obj.childNodes[i].checked) {
                    getstr += obj.childNodes[i].name + "=" + obj.childNodes[i].value + "&";
                }
            }
        }
        if (obj.childNodes[i].tagName == "SELECT") {
            var sel = obj.childNodes[i];
            getstr += sel.name + "=" + sel.options[sel.selectedIndex].value + "&";
        }

    }
    makeEditComponentRequest(pageFolderPath, componentId, getstr, newTitle);
}

function makeEditComponentRequest(pageFolderPath, componentId, arguments, newTitle) {
    var url = "/sf/page/do/editComponent/" + pageFolderPath + arguments;
    var http = window.XMLHttpRequest ? new XMLHttpRequest() :
        new ActiveXObject("Microsoft.XMLHTTP");
    http.open('get', url);
    http.onreadystatechange = function () {
        if (http.readyState == 4) {
            var response = http.responseText;
            if (response.match("success")) {
                editSuccessful(componentId, newTitle);
            } else {
                alert("error");
            }
        }
    };
    http.send(null);
    return false;
}

function editSuccessful(componentId, newTitle) {
    alert("it worked! " + newTitle);
}

function toggleDisplayUsingIds(imageId, baseId) {
    var image = null;
    if (imageId) {
        image = document.getElementById(imageId);
    }
    toggleDisplay(image, baseId);
}

function toggleDisplayNoImage(baseId) {
    var tableObject = document.getElementById(baseId);

    if (tableObject.style.display == 'table' ||
            tableObject.style.display == 'block' ||
            tableObject.style.display == '') {
        tableObject.style.display = 'none';
    } else {
        if (document.all) {
            tableObject.style.display = '';
        } else {
            tableObject.style.display = '';
        }
    }
}

// sets the focus of a field, if the panel is visible
function toggleFormFocus(fieldId, panelId) {
    var panelObject = document.getElementById(panelId);

    if (panelObject.style.display == 'none')
        return;
    var fieldObject = document.getElementById(fieldId);
    fieldObject.focus();
}

// pce: end


//function that checks the element to see if the reload has failed (if that is the case, the action should have
// responded with the string 'failed'. If it has, reload the current page
//(used for AJAX actions)
function checkResult(element) {
    if ('failed' == element.innerHTML) {
        window.location.reload();
    }
}

/**
* Parses the url and return an array of the url params.  Each entry of the array is an array of two entries (param
* name and param value).
* @param url
* @return array of params
*/
function parseUrlParams(url) {
    var params = new Array();
    var paramStartIndex = url.indexOf("?");
    if (paramStartIndex != -1) {
        // means there are params
        var paramStr = url.substring(paramStartIndex + 1);
        var paramPairs = paramStr.split("&");
        for (var i = 0; i < paramPairs.length; i++) {
            var keypair = paramPairs[i].split("=");
            // use decodeURI since 'unescape' does not work properly for non-ASCII characters
            for (var j = 0; j < keypair.length; j++) {
                keypair[j] = decodeURI(keypair[j]);
            }
            params[params.length] = keypair;
        }
    }
    return params;
}

/* firefox doesn't always update the status bar after loading flash. */
function firefoxFinishLoadHack() {
    setTimeout(loadImgHack, 1500);
}

function clearImgHack() {
    var pageLoadedEl = document.getElementById('PageLoaded')
    if (pageLoadedEl) {
        pageLoadedEl.innerHTML = " ";
    }
}

function loadImgHack() {
    var pageLoadedEl = document.getElementById('PageLoaded')
    if (pageLoadedEl) {
        pageLoadedEl.innerHTML = '<img src="/sf-images/misc/white.gif" alt=""></img>';
    }
}

function printWindowWithDelay() {
    setTimeout("if (window.print) window.print();", 100);
}

function checkEmptyContent(fieldId, errorMsg) {
    var content;
    content = document.getElementById(fieldId).value;
    if (content == "") {
        alert(errorMsg);
        return false;
    }
    return true;
}

