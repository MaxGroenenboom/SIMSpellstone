VALID_HEROES = [
	'gmQAA', // Samael
	'4!fAA', // Tarian the Lich Lord
	'QXvAA', // Groc the Hammer
	'Yf0AA', // Rayne the Wavecrasher
	'gn5AA', // Orgoth the Hex Fist
	'ov!AA', // Ol' Cedric
	'w3DBA', // Oda the Aegis
	'4~IBA', // Uriel the Manashifter
	'AIOBA', // Aria the Nightwielder
	'IQTBA', // Decim the Pyrokinetic
	'QYYBA', // Elyse the Truestriker
	'YgdBA', // General Ursario
];

$(() => {
	$("#btn_bf_remove").click(bruteforceRemove);
	$("#btn_bf_heroes").click(bruteforceHeroes);
});

function bruteforceRemove() {
	var realDeck = $('#deck1').val();
	var hero = realDeck.slice(0,5);
	var deck = realDeck.slice(5);
	var deckArray = [];
	while (deck.length > 0) {
		var nextChar = deck.slice(0,5);
		deckArray.push(nextChar);
		deck = deck.slice(5);
	}
	var waitOnce = true;

	function doBruteforceStep(index) {
		if (index >= deckArray.length) { // Stop when finished.
			$('#deck1').val(realDeck);
			return;
		}
		if ($('#ui').is(":not(:visible)")) { // Skip if simulation running.
			setTimeout(() => {
				doBruteforceStep(index);
			},100);
			return;
		} else if (waitOnce) { // Skip if too fast after simulation.
			waitOnce = false;
			setTimeout(() => {
				doBruteforceStep(index);
			},200);
			return;
		}
		waitOnce = true;

		console.log("Bruteforce step " + index);
		var deck = hero;
		for (var i = 0; i < deckArray.length; i++) {
			if (i != index) {
				deck += deckArray[i];
			}
		}

		$('#deck1').val(deck);
		$('#btn_simulate').click();

		setTimeout(() => {
			doBruteforceStep(index + 1);
		},100);
	}

	doBruteforceStep(0);
	return false;
}

function bruteforceHeroes() {
	var realDeck = $('#deck1').val();
	var hero = realDeck.slice(0,5);
	var rest = realDeck.slice(5);
	var waitOnce = true;

	function doBruteforceStep(index) {
		if (index >= VALID_HEROES.length) { // Stop when finished.
			$('#deck1').val(realDeck);
			return;
		}
		if ($('#btn_simulate').is(":not(:visible)")) { // Skip if simulation running.
			setTimeout(() => {
				doBruteforceStep(index);
			},100);
			return;
		} else if (waitOnce) { // Skip if too fast after simulation.
			waitOnce = false;
			setTimeout(() => {
				doBruteforceStep(index);
			},200);
			return;
		}
		waitOnce = true;

		console.log("Bruteforce step " + index);
		var deck = VALID_HEROES[index] + rest;

		$('#deck1').val(deck);
		$('#btn_simulate').click();

		setTimeout(() => {
			doBruteforceStep(index + 1);
		},100);
	}

	doBruteforceStep(0);
	return false;
}
