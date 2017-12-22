$(() => {
	check_update();
});

function generateHash(string) {
	var hash = 0, i, chr;
	if (string.length === 0) return hash;
	for (i = 0; i < string.length; i++) {
		chr   = string.charCodeAt(i);
		hash  = ((hash << 5) - hash) + chr;
		hash |= 0; // Convert to 32bit integer
	}
	return hash;
}

var oldHash = 0;
function check_update() {
	var hash = generateHash(localStorage.SavedDecks);
	if (oldHash != hash) {
		oldHash = hash;
		update();
	}
	setTimeout(check_update, 100);
}

function update() {
	var decks = storageAPI.getSavedDecks();
	var deckContainer = $("#decksContainer");
	var processDeck = {};
	processDeck["decks-item"] = function(name) {
		var newHash = storageAPI.loadDeck(name);
		$("#hash").val(newHash).change();
	}
	processDeck["deck-name"] = processDeck["decks-item"];
	processDeck["save-button"] = function(name) {
		var hash = $("#hash").val();
		storageAPI.saveDeck(name, hash);
	}
	processDeck["delete-button"] = function(name) {
		storageAPI.deleteDeck(name);
	}

	deckContainer.empty();
	for (var deckName in decks) {
		$('<div class="decks-item"><span class="deck-name">' + deckName + '</span><div class="save-button button ui-icon ui-icon-disk" /><div class="delete-button button ui-icon ui-icon-trash" /></div>').appendTo($("<div></div>").appendTo(deckContainer)).click((e) => {
			var target = e.target.classList[0];
			var name = $(e.target.parentNode).find(".deck-name").html();
			processDeck[target](name);
		});
	}
}

