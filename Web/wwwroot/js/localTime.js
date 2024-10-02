window.localTime = {};

window.localTime = {
	updateTag: async function (htmlNode) {
		var data = await DotNet.invokeMethodAsync(
			'Web',
			'DateFormat',
			htmlNode.attributes["data-time"].value,
			new Date().getTimezoneOffset(),
			htmlNode.attributes["data-format"].value);
		htmlNode.innerHTML = data;
	},
	updateAllTags: async function () {
		//convert existing display tags
		var timeTags = document.getElementsByClassName("local-time")
		for (var i = 0; i < timeTags.length; i++) {
			await this.updateTag(timeTags[i]);
		}
	}
};
