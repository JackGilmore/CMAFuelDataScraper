---
layout: default
title: "Fuel Retailers & Stations"
---

<div class="container my-4">
	<h1 class="mb-2">UK Fuel Retailers & Stations</h1>
	<p class="lead text-muted mb-4">Explore major fuel retailers and view petrol stations on the map below. Click a retailer card to filter stations.</p>

	<!-- Retailer cards will be injected here -->
	<div id="retailers" class="row g-2 mb-3"></div>

	<!-- Map with location button -->
	<div style="position: relative;">
		<div id="map" class="w-100" style="height:60vh"></div>
		<button id="locate-btn" class="btn btn-primary" title="Go to my location">
			<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" viewBox="0 0 16 16">
				<path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10zm0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6z"/>
			</svg>
		</button>
	</div>

	<div id="status" class="mt-2 text-muted small"></div>
</div>

<!-- The layout includes scripts for Leaflet, Bootstrap and will load /assets/js/main.js -->
