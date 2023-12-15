// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

console.log(graphData); // debugging

function renderGraph() {

    // Check if graphData is defined
    if (!graphData || !graphData.Nodes || !graphData.Edges) {
        console.error('Graph data is not defined or is missing nodes or edges');
        return; // Stop the execution of the script if data is not valid
    }

    const nodes = graphData.Nodes;
    const links = graphData.Edges;

    console.log("Nodes:", nodes);
    console.log("Links:", links);

    const svg = d3.select("#graphContainer").append("svg")
        .attr("width", "100%")
        .attr("height", "100%")
        .attr("viewBox", "0 0 1200 1200") // centering & view box
        .attr("preserveAspectRatio", "xMidYMid meet");

    const g = svg.append("g"); // adding a g element

    // zoom function
    const zoom = d3.zoom()
        .scaleExtent([0.1, 4]) // Set the minimum and maximum zoom scale
        .on("zoom", (event) => {
            g.attr("transform", event.transform); // Apply the zoom transformation
        });

    svg.call(zoom);


    // Define arrowhead markers
    svg.append("defs").selectAll("marker")
        .data(["end"]) // Unique identifier for the marker
        .enter().append("marker")
        .attr("id", String)
        .attr("viewBox", "0 -5 10 10")
        .attr("refX", 15) // adjust this depending on the size of the nodes, should work
        .attr("refY", 0)
        .attr("markerWidth", 6)
        .attr("markerHeight", 6)
        .attr("orient", "auto")
        .append("path")
        .attr("fill", "#999") // Arrow color
        .attr("d", "M0,-5L10,0L0,5");

    // Create a simulation for positioning nodes
    const simulation = d3.forceSimulation(nodes)
        .force("link", d3.forceLink(links).id(d => d.Id).distance(() => Math.random() * 100 + 70)) // randomly adjust distance
        .force("charge", d3.forceManyBody().strength(-50)) // you can adjust strength for repulsion
        .force("center", d3.forceCenter(600, 300)); // centered horizontally on the viewbox.

    // Draw lines for links (edges) with arrowheads
    const link = g.append("g") // append to the g element for zoom
        .attr("stroke", "#999")
        .attr("stroke-opacity", 0.6)
        .selectAll("path")
        .data(links)
        .join("path")
        .attr("marker-end", "url(#end)") // use the defined arrow marker
        .attr("fill", "none")
        .attr("stroke-width", 2);

    // Draw circles for nodes
    const node = g.append("g") // append to the g element for zoom
        .attr("stroke", "#fff")
        .attr("stroke-width", 1.5)
        .selectAll("circle")
        .data(nodes)
        .join("circle")
        .attr("r", d => d.Size) // this is the size determined by the size property default can be set to("r", 10)
        .attr("fill", d => d.Color) // use 'Color' property
        .call(drag(simulation))
        node.on("mouseover", function (event, d) {
            g.append("text")
                .attr("x", d.x + 10)
                .attr("y", d.y)
                .text(d.Id)
                .attr("id", "hoverLabel") // use the id for styling or replace id with class for styling #hoverLabel vs .hoverLabel
                .attr("class", "hover-text"); // or just use this class LOL
        })
        .on("mouseout", function () {
            svg.select("#hoverLabel").remove();
        });

    // Add labels to nodes
    const labels = g.append("g") // append to the g element for zoom
        .attr("class", "labels")
        .selectAll("text")
        .data(nodes)
        .enter()
        .append("text")
        .attr("dx", 12)
        .attr("dy", ".35em");
    // .text(d => d.Id); i commented this code out. If we add this code, it will show all the labels.

    // Update positions on each simulation tick (CURVED LINE METHOD)
    // uncomment this code and remove the above code if you want curved lines
    /*
    simulation.on("tick", () => {
        link.attr("d", d => {
            const dx = d.target.x - d.source.x,
                dy = d.target.y - d.source.y,
                dr = Math.sqrt(dx * dx + dy * dy);
            return `M${d.source.x},${d.source.y}A${dr},${dr} 0 0,1 ${d.target.x},${d.target.y}`;
        });

        node.attr("cx", d => d.x)
            .attr("cy", d => d.y);

        labels.attr("x", d => d.x)
            .attr("y", d => d.y);
    });
    */

    // Update link positions on each simulation tick (STRAIGHT LINE METHOD)
    simulation.on("tick", () => {
        link.attr("d", d => `M${d.source.x},${d.source.y}L${d.target.x},${d.target.y}`);

        node.attr("cx", d => d.x)
            .attr("cy", d => d.y);

        labels.attr("x", d => d.x)
            .attr("y", d => d.y);
    });

    // Drag functionality for nodes
    function drag(simulation) {
        function dragstarted(event, d) {
            if (!event.active) simulation.alphaTarget(0.3).restart();
            d.fx = d.x;
            d.fy = d.y;
        }

        function dragged(event, d) {
            d.fx = event.x;
            d.fy = event.y;
        }

        function dragended(event, d) {
            if (!event.active) simulation.alphaTarget(0);
            d.fx = null;
            d.fy = null;
        }

        return d3.drag()
            .on("start", dragstarted)
            .on("drag", dragged)
            .on("end", dragended);
    }

    // reset zoom functionality
    document.getElementById("resetZoom").addEventListener("click", function () {
        svg.transition()
            .duration(750) // smooth transition
            .call(zoom.transform, d3.zoomIdentity); // Reset zoom
    });
}

renderGraph();