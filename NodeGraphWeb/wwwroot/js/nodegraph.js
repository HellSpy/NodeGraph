// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

console.log(graphData); // debugging
function renderGraph() {
    // Check if graphData is defined
    if (!graphData || !graphData.Nodes || !graphData.Edges) {
        console.error('Graph data is not defined or is missing nodes or edges');
        return;
    }

    const nodes = graphData.Nodes;
    const links = graphData.Edges;

    // Set canvas to take full size of its container
    const container = d3.select("#graphContainer");
    const width = container.node().getBoundingClientRect().width;
    const height = container.node().getBoundingClientRect().height;

    // Create a canvas element
    const canvas = container.append("canvas")
        .attr("width", width)
        .attr("height", height);

    const context = canvas.node().getContext("2d");
    let transform = d3.zoomIdentity;

    const radius = 100; // Define the desired radius for your nodes - this is for the circle in which all the nodes will be in

    // Create a simulation for positioning nodes with adjusted parameters for faster movement
    const simulation = d3.forceSimulation(nodes)
        .force("link", d3.forceLink(links).id(d => d.Id).distance(() => Math.random() * 100 + 70)) // randomly adjust distance
        .force("charge", d3.forceManyBody()
            .strength(d => -30 * (d.Size || 1)) // Adjust strength based on node size
        )
        .force("collide", d3.forceCollide()
            .radius(d => (d.Size * 1.1)) // dynamic radius based on half of the node's size, plus a buffer of 1
            .strength(2)) // maximum strength to ensure separation
        .force("center", d3.forceCenter(width / 2, height / 2))
        .force("radial", d3.forceRadial(radius, width / 2, height / 2).strength(0.4)) // add the radial force
        .alphaDecay(0.05)
        .on("tick", () => { render(); });

    // Zoom functionality adapted for canvas
    const zoom = d3.zoom()
        .scaleExtent([0.1, 10]) // Adjusted for a wider zoom range
        .on("zoom", (event) => {
            transform = event.transform;
            render();
        });

    canvas.call(zoom);

    // Drag functionality for nodes
    function dragsubject(event) {
        let i, node, dx, dy;
        const x = transform.invertX(event.x),
            y = transform.invertY(event.y);
        for (i = nodes.length - 1; i >= 0; --i) {
            node = nodes[i];
            dx = x - node.x;
            dy = y - node.y;
            if (dx * dx + dy * dy < 30) {
                node.x = transform.applyX(node.x);
                node.y = transform.applyY(node.y);
                return node;
            }
        }
    }

    const drag = d3.drag()
        .container(canvas.node())
        .subject(dragsubject)
        .on("start", dragstarted)
        .on("drag", dragged)
        .on("end", dragended);

    canvas.call(drag);

    function dragstarted(event) {
        if (!event.active) simulation.alphaTarget(0.3).restart();
        event.subject.fx = transform.invertX(event.x);
        event.subject.fy = transform.invertY(event.y);
    }

    function dragged(event) {
        event.subject.fx = transform.invertX(event.x);
        event.subject.fy = transform.invertY(event.y);
    }

    function dragended(event) {
        if (!event.active) simulation.alphaTarget(0);
        event.subject.fx = null;
        event.subject.fy = null;
    }

    function getConnectedNodes(node) {
        const connectedNodes = new Set();
        links.forEach(link => {
            if (link.source.Id === node.Id || link.target.Id === node.Id) {
                connectedNodes.add(link.source.Id);
                connectedNodes.add(link.target.Id);
            }
        });
        return connectedNodes;
    }

    function isLinkConnected(link, connectedNodes) {
        return connectedNodes.has(link.source.Id) || connectedNodes.has(link.target.Id);
    }

    canvas.on("mousemove", mousemoved);

    let connectedLinks = new Set();
    let connectedNodes = new Set();


    function mousemoved(event) {
        const mouseX = transform.invertX(event.offsetX);
        const mouseY = transform.invertY(event.offsetY);

        let hoverNode = null;
        for (let node of nodes) {
            const dx = mouseX - node.x;
            const dy = mouseY - node.y;
            // Ensure you are using the correct size property from your node data
            if (dx * dx + dy * dy < (node.Size || 5) * (node.Size || 5)) {
                hoverNode = node;
                break;
            }
        }

        connectedLinks.clear();
        if (hoverNode) {
            console.log("Hovering over node:", hoverNode);
            showTooltip(hoverNode, event.offsetX, event.offsetY);
            connectedNodes = getConnectedNodes(hoverNode);

            // Identify connected links
            links.forEach(link => {
                if (link.source.Id === hoverNode.Id || link.target.Id === hoverNode.Id) {
                    connectedLinks.add(link);
                }
            });
        } else {
            hideTooltip();
            connectedNodes.clear();
        }
        render();  // re-render to update the opacities
    }

    function showTooltip(node, mouseX, mouseY) {
        const canvasRect = canvas.node().getBoundingClientRect(); // Get the bounding rectangle of the canvas

        // Apply the current transformation to the node's position
        const transformedX = transform.applyX(node.x) + canvasRect.left;
        const transformedY = transform.applyY(node.y) + canvasRect.top;

        const xOffset = 10; // Horizontal offset from the node
        const yOffset = 10; // Vertical offset from the node

        const tooltip = d3.select("#tooltip");
        tooltip.style("left", (transformedX + xOffset) + "px")
            .style("top", (transformedY + yOffset) + "px")
            .html(node.Id) // Use the property that contains the text you want to display
            .style("visibility", "visible");
    }


    function hideTooltip() {
        const tooltip = d3.select("#tooltip");
        tooltip.style("visibility", "hidden");
    }

    // Reset zoom button event listener
    document.getElementById('resetZoom').addEventListener('click', () => {
        transform = d3.zoomIdentity;
        render();
        canvas.transition().duration(750).call(zoom.transform, d3.zoomIdentity);
    });

    // Render function to draw the graph
    function render() {
        context.clearRect(0, 0, width, height);
        context.save();
        context.translate(transform.x, transform.y);
        context.scale(transform.k, transform.k);

        // Draw links
        links.forEach(function (d) {
            context.beginPath();
            context.moveTo(d.source.x, d.source.y);
            context.lineTo(d.target.x, d.target.y);
            // Check if we are hovering over a node and if the link is connected to it
            const isLinkDimmed = connectedNodes.size > 0 && !connectedLinks.has(d);
            context.strokeStyle = isLinkDimmed ? "rgba(170, 170, 170, 0.2)" : "#aaa";
            context.stroke();
        });

        // Draw nodes
        nodes.forEach(function (d) {
            context.beginPath();
            context.arc(d.x, d.y, d.Size || 5, 0, 2 * Math.PI); // Use 'Size' attribute, default to 5
            context.fillStyle = d.Color; // Use 'Color' attribute
            context.globalAlpha = connectedNodes.size === 0 || connectedNodes.has(d.Id) ? 1 : 0.1; // Adjust opacity
            context.fill();
        });
        context.globalAlpha = 1; // Reset globalAlpha to default
        context.restore();
    }
}

renderGraph();

// Add the SVG for the legend
function addLegend() {
    const svg = d3.select("#graphContainer").append("svg")
        .attr("width", 300) // Adjust width and height as needed
        .attr("height", 100)
        .style("position", "absolute")
        .style("top", "10px")
        .style("left", "10px");

    // Legend code
    function defineGradient(svg) {
        const gradient = svg.append("defs")
            .append("linearGradient")
            .attr("id", "legendGradient")
            .attr("x1", "0%")
            .attr("x2", "100%")
            .attr("y1", "0%")
            .attr("y2", "0%");

        // Define the gradient stops
        gradient.append("stop")
            .attr("offset", "0%")
            .attr("stop-color", "blue");

        gradient.append("stop")
            .attr("offset", "25%")
            .attr("stop-color", "green");

        gradient.append("stop")
            .attr("offset", "50%")
            .attr("stop-color", "red");

        gradient.append("stop")
            .attr("offset", "75%")
            .attr("stop-color", "purple");

        gradient.append("stop")
            .attr("offset", "100%")
            .attr("stop-color", "pink");
    }

    function createGradientLegend(svg) {
        const legendWidth = 200; // Width of the gradient bar
        const legendHeight = 20; // Height of the gradient bar
        const legendX = 0; // X position of the legend
        const legendY = 0; // Y position of the legend

        // Container for the legend
        const legend = svg.append("g")
            .attr("class", "legend")
            .attr("transform", `translate(${legendX}, ${legendY})`); // Position at top-left corner

        // White background
        legend.append("rect")
            .attr("x", 0)
            .attr("y", 0)
            .attr("width", legendWidth + 60) // Extra width for text
            .attr("height", legendHeight + 40) // Extra height for text and padding
            .attr("fill", "white");

        // Gradient bar
        legend.append("rect")
            .attr("x", 20)
            .attr("y", 20)
            .attr("width", legendWidth)
            .attr("height", legendHeight)
            .style("fill", "url(#legendGradient)");

        // Text labels for the gradient
        const textData = [
            { position: "start", text: "Fewer links" },
            { position: "end", text: "More links" }
        ];

        legend.selectAll("text")
            .data(textData)
            .enter()
            .append("text")
            .attr("x", (d, i) => i === 0 ? 20 : legendWidth + 40)
            .attr("y", 15)
            .attr("text-anchor", (d, i) => i === 0 ? "start" : "end")
            .text(d => d.text);
    }

    defineGradient(svg);
    createGradientLegend(svg);
}

addLegend();