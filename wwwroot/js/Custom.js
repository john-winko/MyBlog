﻿let index = 0;

function AddTag() {
    // get a reference to the TagEntry input element (above the add/delete buttons)
    var tagEntry = document.getElementById("TagEntry");

    // Create a new select option
    let newOption = new Option(tagEntry.value, tagEntry.value);
    document.getElementById("TagList").options[index++] = newOption;

    // Clear out TagEntry control
    tagEntry.value = "";
    return true;

}

function DeleteTag() {
    let tagCount = 1;

    while (tagCount > 0) {
        let tagList = document.getElementById("TagList");
        let selectedIndex = tagList.selectedIndex;
        if (selectedIndex >= 0) {
            tagList.options[selectedIndex] = null;
            tagCount--;
        } else {
            tagCount = 0;
        }
        index--;
    }
}

$("form").on("submit", function() {
    $("#TagList option").prop("selected", "selected");
});

// Look for tagValues if it has data
if (tagValues != "") {
    let tagArray = tagValues.split(",");
    for (let loop = 0; loop < tagArray.length; loop++) {
        // Load up or replace options we have
        ReplaceTag(tagArray[loop], loop);
        index++;
    }
}

function ReplaceTag(tag, index) {
    let newOption = new Option(tag, tag);
    document.getElementById("TagList").options[index] = newOption;
}