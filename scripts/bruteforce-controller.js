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

SIMULATION_DELAY = 200;

$(() => {
	elem = {
		deckInput: $('#deck1'),
		ui: $('#ui'),
		simulateBtn: $('#btn_simulate'),
		header: $('header'),
		additions: $('#field_possible_additions'),
	};

	$("#btn_bf_remove").click(bruteforceRemove);
	$("#btn_bf_heroes").click(bruteforceHeroes);
	$("#btn_bf_adds").click(bruteforceAdditions);
});

bruteforcing = false;

function splitDeck(deck = "") {
	var deckArray = [];
	while (deck.length > 0) {
		var nextCard = deck.slice(0,5);
		deckArray.push(nextCard);
		deck = deck.slice(5);
	}
	return deckArray;
}

/**
 * Does a bruteforce simulation using the supplied functions.
 *
 * @param {function} init The function called before the simulation.
 *     Should accept the input deck as string.
 * @param {function} step The function called for each step in the simulation.
 *     Should accept an int as parameter: the nth step of the bruteforce.
 * @param {function} condition The function called to determine the end of simulation.
 *     Should accept an int as parameter: the nth step of the bruteforce.
 */
function startBruteforce(init, step, condition) {
	// Create deck input for init.
	var realDeck = elem.deckInput.val();
	init(realDeck);

	// Create helper functions.
	function startBruteforce() { // Called when bruteforcing starts.
		bruteforcing = true;
		elem.header.addClass("bruteforcing");
	}
	function endBruteforce() { // Called when bruteforcing finishes.
		bruteforcing = false;
		elem.header.removeClass("bruteforcing");
	}
	function doStep(index) { // Recursive step function of the algorithm.
		if (condition(index)) { // Stops recursion when stop condition is reached.
			elem.deckInput.val(realDeck);
			endBruteforce();
			return;
		}
		if (elem.ui.is(":not(:visible)")) { // Wait when simulation is still running.
			setTimeout(() => doStep(index), 100);
			return;
		}
		if (waitOnce) { // Ensure delay between simulations.
			waitOnce = false;
			setTimeout(() => doStep(index), SIMULATION_DELAY);
			return;
		} else {
			waitOnce = true;
		}

		// Do simulation modification.
		console.log("Starting bruteforce step " + (index+1));
		var bruteforceDeck = step(index);
		elem.deckInput.val(bruteforceDeck);
		elem.simulateBtn.click();

		// Continue to the next step.
		setTimeout(() => doStep(index + 1), 100);
	};

	// Start the recursive bruteforce algorithm.
	console.log("Initiating bruteforce");
	startBruteforce();
	var waitOnce = true;
	doStep(0);
	return false;
}

function bruteforceRemove() {
	var hero;
	var deckArray;

	function init(d) {
		var deck = splitDeck(d);
		hero = deck[0];
		deckArray = deck.slice(1);
	}
	function step(index) {
		var deck = hero;
		for (var i = 0; i < deckArray.length; i++) {
			if (i != index) {
				deck += deckArray[i];
			}
		}
		return deck;
	}
	function condition(index) {
		return index >= deckArray.length;
	}
	return startBruteforce(init, step, condition);
}

function bruteforceHeroes() {
	var hero;
	var cards = "";

	function init(d) {
		hero = d.slice(0,5);
		cards = d.slice(5);
	}
	function step(index) {
		var deck = VALID_HEROES[index] + cards;
		return deck;
	}
	function condition(index) {
		return index >= VALID_HEROES.length;
	}
	return startBruteforce(init, step, condition);
}

function bruteforceAdditions() {
	var cards;
	var additions = splitDeck(elem.additions.val().slice(5));

	function init(d) {
		cards = d;
	}
	function step(index) {
		var deck = cards + additions[index];
		return deck;
	}
	function condition(index) {
		return index >= additions.length;
	}
	return startBruteforce(init, step, condition);
}

