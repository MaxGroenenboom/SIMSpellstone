SHORTCUTS = [
	[1, 3000, 1503, 7],
	[2, 3001, 1507, 7],
	[3, 3002, 1511, 7],
	[4, 3009, 5623, 7],
	[5, 3010, 5687, 7],
	[7, 1111, 5469, 7],
	[8, 1117, 5524, 7],
	[9, 1127, 5594, 7],
	[10, 1133, 5653, 7],
	[11, 1139, 5683, 7],
	[12, 1146, 5745, 7],
	[13, 1153, 5805, 7],
]

function get() {
	return [parseInt(elem.map.val()), parseInt(elem.campaign.val()), parseInt(elem.mission.val()), parseInt(elem.stage.val())];
}

function setMission(mission) {
	console.log("setshortcut: " + mission);
	var mission = SHORTCUTS[mission];
	elem.map.val(mission[0]);
	elem.map.change();
	elem.campaign.val(mission[1]);
	elem.campaign.change();
	elem.mission.val(mission[2]);
	elem.mission.change();
	elem.stage.val(mission[3]);
	elem.stage.change();
}

$(() => {
	if (!window.elem) {
		elem = {};
	}
	elem.map = $('#location');
	elem.campaign = $('#campaign');
	elem.mission = $('#mission');
	elem.stage = $("#mission_level");

	$("#btn_shortcuts_atlas").click(() => setMission(0));
	$("#btn_shortcuts_solaron").click(() => setMission(1));
	$("#btn_shortcuts_vulcanos").click(() => setMission(2));
	$("#btn_shortcuts_void").click(() => setMission(3));
	$("#btn_shortcuts_third").click(() => setMission(4));
	$("#btn_shortcuts_scylla").click(() => setMission(5));
	$("#btn_shortcuts_infinity").click(() => setMission(6));
	$("#btn_shortcuts_zhulong").click(() => setMission(7));
	$("#btn_shortcuts_hopper").click(() => setMission(8));
	$("#btn_shortcuts_pharite").click(() => setMission(9));
	$("#btn_shortcuts_vali").click(() => setMission(10));
	$("#btn_shortcuts_eos").click(() => setMission(11));
});
