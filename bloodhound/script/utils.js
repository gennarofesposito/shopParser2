//
// Parses Json and makes a table which it adds to the body
// Useful defaul for parsing JSON onto the page
//
function getData (target) {
  $.getJSON(target, function (data) {
    var items = [];
    var headers = "";
    $.each(data, function (key, val) {
      var fields = "";
      var ii = 0;
      $.each(val, function (i, n) {
        // do headers once - give them Ids in case we wwant to auto click them for sorting
        if (items.length < 1) {
          headers += '<th id="col' + ii + '">' + i + '</th>'
        }
        fields += '<td>' + n + '</td>';
        ii++;
      });
      items.push('<tr>' + fields + '</tr>');
    });
    // add the header row at the start
    items.unshift('<thead><tr>' + headers + '</tr></thead>');
    $('<table />', {
      'class': 'sortable',
      html: items.join('')
    }).appendTo('body');
    init();
  });
}

//
// Get a the value of specified parameter for the Qstring of the page
//
function getParameterByName(name) {
  name = name.replace(/[\[]/, "\\\[").replace(/[\]]/, "\\\]");
  var regexS = "[\\?&]" + name + "=([^&#]*)";
  var regex = new RegExp(regexS);
  var results = regex.exec(window.location.href);
  if (results == null)
    return "";
  else
    return decodeURIComponent(results[1].replace(/\+/g, " "));
}

//
// Extend Jscript Date object by adding another Week primitive!
//
Date.prototype.getWeek = function () {
  var onejan = new Date(this.getFullYear(), 0, 1);
  return Math.ceil((((this - onejan) / 86400000) + onejan.getDay() + 1) / 7);
}

//
// Makes a table sortable
//
function makeSortable() {
    var t = new SortableTable(document.getElementsByTagName("table")[0], 100);
}

//
// Used for alerting visually that a value in the table is concerning.
// searches a table for a key phrase to match and a threshold value
// when the text matches - if the threshhold is exceeded - it colour the row red
//
function flagWorrying(tableIndex, columnIndexForTextMatch, columnIndexForValueCheck, textMatchText, thresholdValue) {
    var table = document.getElementsByTagName('table')[tableIndex];
    var cell = table.rows[0].cells[0];
    for (var ii = 0; ii < table.rows.length; ii++) {
        var text = table.rows[ii].cells[columnIndexForTextMatch].firstChild.data;
        var value = table.rows[ii].cells[columnIndexForValueCheck].firstChild.data;
        switch (table.rows[ii].cells[columnIndexForTextMatch].firstChild.data) {
            case textMatchText:
                if (table.rows[ii].cells[columnIndexForValueCheck].firstChild.data > thresholdValue) {
                    table.rows[ii].className = "warning";
                }
                break;
            default:
                // Do nothing
        }

    }
}

//
// Adds a User column
//
function addColumn(deviceProxyColumnOffset) {
  var tblHeadObj = document.getElementsByTagName("table")[0].tHead;
  //var tblHeadObj = document.getElementById(tblId).tHead;
  for (var h = 0; h < tblHeadObj.rows.length; h++) {
    var newTH = document.createElement('th');
    tblHeadObj.rows[h].appendChild(newTH);
    newTH.innerHTML = 'UserLookup';
  }

  var tblBodyObj = document.getElementsByTagName("table")[0].tBodies[0];
  for (var i = 0; i < tblBodyObj.rows.length; i++) {
    var newCell = tblBodyObj.rows[i].insertCell(-1);
    newCell.innerHTML = '<a target="_blank" href="./userLookup.htm?deviceProxyId=' + tblBodyObj.rows[i].cells[(tblBodyObj.rows[i].cells.length - 1) - deviceProxyColumnOffset].innerHTML + '">userLookup</a>'
  }
}


