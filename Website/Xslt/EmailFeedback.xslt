<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
								xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
								xmlns:msxsl="urn:schemas-microsoft-com:xslt"
								xmlns:script="urn:script"
								exclude-result-prefixes="msxsl script">

	<xsl:output method="html"
							indent="no"
							omit-xml-declaration="yes" />

	<xsl:template match="Root">
		<xsl:value-of select="script:SetData('Subject', 'Aion recipe calculator feedback')"/>
		From: <xsl:value-of select="Email"
									disable-output-escaping="yes"/>
		<br />
		<br />
		<br />
		<xsl:value-of select="Message"
									disable-output-escaping="yes"/>
		<br />
		<br />
		<br />
		Additional info:
		<br />
		<br />
		<xsl:value-of select="AdditionalInfo"
									disable-output-escaping="yes"/>

		<xsl:if test="Browser">
			<br />
			<br />
			Browser: <xsl:value-of select="Browser" />
		</xsl:if>
		<xsl:if test="UserAgent">
			<br />
			<br />
			User agent: <xsl:value-of select="UserAgent" />
		</xsl:if>
		<xsl:if test="UserHostAddress">
			<br />
			<br />
			User address: <xsl:value-of select="UserHostAddress" />
		</xsl:if>
	</xsl:template>

</xsl:stylesheet>
